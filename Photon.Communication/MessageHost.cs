using Photon.Communication.Messages;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Photon.Communication
{
    /// <summary>
    /// An incoming TCP message connection.
    /// </summary>
    public class MessageHost
    {
        public event EventHandler<UnhandledExceptionEventArgs> ThreadException;
        public event EventHandler Stopped;

        public MessageTransceiver Transceiver {get;}
        private readonly TaskCompletionSource<bool> handshakeResult;
        private bool isConnected;

        public TcpClient Tcp {get;}


        public MessageHost(TcpClient client, MessageProcessorRegistry registry)
        {
            this.Tcp = client;

            isConnected = true;
            handshakeResult = new TaskCompletionSource<bool>();

            Transceiver = new MessageTransceiver(registry) {
                Context = this,
            };

            Transceiver.ThreadException += Transceiver_OnThreadException;

            var stream = client.GetStream();
            Transceiver.Start(stream);
        }

        public void Dispose()
        {
            Transceiver?.Dispose();
            Tcp?.Close();
        }

        public void Stop(CancellationToken token = default)
        {
            if (!isConnected) return;
            isConnected = false;

            handshakeResult.TrySetCanceled();

            try {
                Transceiver.Flush(token);
                Transceiver.Stop();
            }
            catch {}

            try {
                Tcp.Close();
            }
            catch {}

            OnStopped();
        }

        public void SendOneWay(IRequestMessage message)
        {
            Transceiver.SendOneWay(message);
        }

        public MessageTask Send(IRequestMessage message)
        {
            return Transceiver.Send(message);
        }

        public async Task<bool> GetHandshakeResult(CancellationToken token)
        {
            return await handshakeResult.Task;
        }

        public void CompleteHandshake(bool result)
        {
            handshakeResult.SetResult(result);
        }

        protected virtual void OnStopped()
        {
            Stopped?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnThreadException(Exception exception)
        {
            var e = new UnhandledExceptionEventArgs(exception);
            ThreadException?.Invoke(this, e);
        }

        private void Transceiver_OnThreadException(object sender, UnhandledExceptionEventArgs e)
        {
            OnThreadException(e.Exception);
        }
    }
}
