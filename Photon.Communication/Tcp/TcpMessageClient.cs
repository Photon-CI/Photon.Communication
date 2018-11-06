using Photon.Communication.Messages;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Photon.Communication.Tcp
{
    /// <inheritdoc />
    /// <summary>
    /// Connects to a remote MessageHost.
    /// </summary>
    public class TcpMessageClient : IDisposable
    {
        public event EventHandler<UnhandledExceptionEventArgs> ThreadException;

        public TcpClient Tcp {get;}
        public MessageTransceiver Transceiver {get;}

        public bool IsConnected => Transceiver?.IsStarted ?? false;


        public TcpMessageClient(MessageProcessorRegistry registry)
        {
            Tcp = new TcpClient {
                NoDelay = true,
                ExclusiveAddressUse = false,
                Client = {
                    NoDelay = true,
                    ExclusiveAddressUse = false
                },
            };

            Transceiver = new MessageTransceiver(registry) {
                Context = this,
            };

            Transceiver.ThreadException += Transceiver_OnThreadException;
        }

        public void Dispose()
        {
            Transceiver?.Dispose();
            Tcp?.Close();
        }

        public async Task ConnectAsync(string hostname, int port, CancellationToken token)
        {
            using (token.Register(() => Tcp.Close())) {
                await Tcp.ConnectAsync(hostname, port);
            }

            var stream = Tcp.GetStream();
            Transceiver.Start(stream);
        }

        public void Disconnect(int seconds = 30)
        {
            var timeout = TimeSpan.FromSeconds(seconds);
            Disconnect(timeout);
        }

        public void Disconnect(TimeSpan timeout)
        {
            try {
                using (var tokenSource = new CancellationTokenSource(timeout)) {
                    Transceiver.Flush(tokenSource.Token);
                    Transceiver.Stop();
                }
            }
            catch {}

            try {
                Tcp.Close();
            }
            catch {}
        }

        public async Task<TResponse> Handshake<TResponse>(IRequestMessage handshakeRequest, TimeSpan timeout, CancellationToken token)
            where TResponse : class, IResponseMessage
        {
            using (var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(token)) {
                tokenSource.CancelAfter(timeout);

                return await Send(handshakeRequest)
                    .GetResponseAsync<TResponse>(tokenSource.Token);
            }
        }

        public MessageTask Send(IRequestMessage message)
        {
            return Transceiver.Send(message);
        }

        public void SendOneWay(IRequestMessage message)
        {
            Transceiver.SendOneWay(message);
        }

        protected virtual void OnThreadException(Exception exception)
        {
            ThreadException?.Invoke(this, new UnhandledExceptionEventArgs(exception));
        }

        private void Transceiver_OnThreadException(object sender, UnhandledExceptionEventArgs e)
        {
            OnThreadException(e.Exception);
        }
    }
}
