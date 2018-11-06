using System.Threading.Tasks;
using Photon.Communication.Messages;
using Photon.Communication.Tests.Internal.TRx;
using Xunit;

namespace Photon.Communication.Tests
{
    public class DisconnectTests
    {
        private readonly MessageProcessorRegistry registry;


        public DisconnectTests()
        {
            registry = new MessageProcessorRegistry();
            registry.Register(typeof(HostDisconnectProcessor));
        }

        [Fact]
        public void HostDisconnectWaits()
        {
            var context = new DisconnectContext {
                Handle = new TaskCompletionSource<object>(),
                Complete = false,
            };

            using (var duplexer = new Duplexer())
            using (var host = new MessageTransceiver(registry))
            using (var client = new MessageTransceiver(registry)) {
                host.Context = context;
                client.Context = context;

                host.Start(duplexer.StreamA);
                client.Start(duplexer.StreamB);

                var message = new HostDisconnectRequest();
                host.Send(message);

                host.Flush();
                host.Stop();

                context.Handle.Task.Wait(600);

                client.Flush();
                client.Stop();

                Assert.True(context.Complete);
            }
        }

        [Fact]
        public async Task ClientDisconnectWaits()
        {
            var context = new DisconnectContext {
                Handle = new TaskCompletionSource<object>(),
                Complete = false,
            };

            using (var duplexer = new Duplexer())
            using (var host = new MessageTransceiver(registry))
            using (var client = new MessageTransceiver(registry)) {
                host.Context = context;
                client.Context = context;

                host.Start(duplexer.StreamA);
                client.Start(duplexer.StreamB);

                var _client = client;
                var clientTask = Task.Run(async () => {
                    await context.Handle.Task;

                    await _client.FlushAsync();
                    _client.Stop();
                });

                var message = new HostDisconnectRequest();
                var result = host.Send(message).GetResponseAsync().Wait(10_000);
                Assert.True(result, "Timeout waiting for response message!");

                await host.FlushAsync();
                host.Stop();

                result = clientTask.Wait(10_000);
                Assert.True(result, "Timeout waiting for client task to complete!");

                Assert.True(context.Complete);
            }
        }

        private class HostDisconnectRequest : IRequestMessage
        {
            public string MessageId {get; set;}
        }

        private class HostDisconnectProcessor : MessageProcessorBase<HostDisconnectRequest>
        {
            public override async Task<IResponseMessage> Process(HostDisconnectRequest requestMessage)
            {
                var context = (DisconnectContext)Transceiver.Context;
                context.Complete = true;
                context.Handle.SetResult(null);

                return await Task.FromResult(new ResponseMessageBase());
            }
        }

        private class DisconnectContext
        {
            public TaskCompletionSource<object> Handle {get; set;}
            public bool Complete {get; set;}
        }
    }
}
