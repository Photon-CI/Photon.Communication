﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Photon.Communication.Tcp
{
    /// <inheritdoc />
    /// <summary>
    /// Listens for incomming TCP message connections, and manages
    /// a collection of <see cref="T:Photon.Communication.MessageHost" /> instances.
    /// </summary>
    public class TcpMessageListener : IDisposable
    {
        public event EventHandler<TcpConnectionReceivedEventArgs> ConnectionReceived;
        public event EventHandler<UnhandledExceptionEventArgs> ThreadException;

        private readonly MessageProcessorRegistry messageRegistry;
        private readonly List<MessageHost> hostList;
        private readonly object startStopLock;
        private TcpListener listener;
        private volatile bool isListening;


        public TcpMessageListener(MessageProcessorRegistry registry)
        {
            messageRegistry = registry;

            hostList = new List<MessageHost>();
            startStopLock = new object();
        }

        public void Dispose()
        {
            foreach (var host in hostList) {
                try {
                    host.Dispose();
                }
                catch {}
            }

            hostList.Clear();

            try {
                listener?.Stop();
            }
            catch {}
            finally {
                listener = null;
            }
        }

        public void Listen(IPAddress address, int port)
        {
            lock (startStopLock) {
                if (isListening) throw new Exception("Host is already listening!");
                isListening = true;
            }

            listener = new TcpListener(address, port) {
                ExclusiveAddressUse = false,
                Server = {
                    NoDelay = true,
                    ExclusiveAddressUse = false,
                }
            };

            listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            listener.Start();

            listener.BeginAcceptTcpClient(Listener_OnConnectionReceived, new object());
        }

        public void Stop(CancellationToken token = default)
        {
            lock (startStopLock) {
                if (!isListening) return;
                isListening = false;
            }

            var _hosts = hostList.ToArray();
            Parallel.ForEach(_hosts, host => {
                try {
                    host.Stop(token);
                }
                catch {}
            });

            try {
                listener.Stop();
            }
            catch {}
        }

        private void Listener_OnConnectionReceived(IAsyncResult result)
        {
            lock (startStopLock) {
                if (!isListening) return;
            }

            TcpClient client;
            try {
                client = listener.EndAcceptTcpClient(result);
            }
            catch (Exception error) {
                OnThreadException(error);
                return;
            }
            finally {
                if (isListening)
                    listener.BeginAcceptTcpClient(Listener_OnConnectionReceived, new object());
            }

            var host = AcceptClient(client);
            BeginOnConnectionReceived(host);
        }

        private MessageHost AcceptClient(TcpClient client)
        {
            var host = new MessageHost(client, messageRegistry);
            host.ThreadException += Host_OnThreadException;
            host.Stopped += Host_Stopped;
            hostList.Add(host);
            return host;
        }

        private void Host_OnThreadException(object sender, UnhandledExceptionEventArgs e)
        {
            OnThreadException(e.Exception);
        }

        private void Host_Stopped(object sender, EventArgs e)
        {
            var host = (MessageHost)sender;
            hostList.Remove(host);
            host.Dispose();
        }

        protected virtual void BeginOnConnectionReceived(MessageHost host)
        {
            if (ConnectionReceived == null) return;

            var args = new TcpConnectionReceivedEventArgs(host);
            ConnectionReceived?.BeginInvoke(this, args, EndOnConnectionReceived, args);
        }

        protected virtual void EndOnConnectionReceived(IAsyncResult state)
        {
            var args = (TcpConnectionReceivedEventArgs)state.AsyncState;

            if (!args.Accept) {
                hostList.Remove(args.Host);
                args.Host.Dispose();
            }
        }

        protected virtual void OnThreadException(Exception exception)
        {
            var e = new UnhandledExceptionEventArgs(exception);
            ThreadException?.Invoke(this, e);
        }
    }

    public class TcpConnectionReceivedEventArgs : EventArgs
    {
        public MessageHost Host {get;}
        public bool Accept {get; set;}


        public TcpConnectionReceivedEventArgs(MessageHost host)
        {
            this.Host = host;

            Accept = true;
        }
    }
}
