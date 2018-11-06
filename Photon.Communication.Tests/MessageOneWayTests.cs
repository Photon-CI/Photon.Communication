using System.Threading;
using System.Threading.Tasks;
using Photon.Communication.Messages;
using Photon.Communication.Tests.Internal.TRx;
using Xunit;

namespace Photon.Communication.Tests
{
    //[Explicit("Flushing one-way messages is not currently guaranteed.")]
    public class MessageOneWayTests
    {
        private readonly MessageProcessorRegistry registry;


        public MessageOneWayTests()
        {
            registry = new MessageProcessorRegistry();
            registry.Register(typeof(TestMessageProcessor));
        }

        [Fact]
        public async Task SendMessageOneWay_ToClient_1000x()
        {
            using (var duplexer = new Duplexer())
            using (var host = new MessageTransceiver(registry))
            using (var client = new MessageTransceiver(registry)) {
                var context = new MessageContext();
                host.Context = context;
                client.Context = context;

                host.Start(duplexer.StreamA);
                client.Start(duplexer.StreamB);

                for (var i = 0; i < 1000; i++)
                    host.SendOneWay(new TestMessage());

                await host.FlushAsync();
                host.Stop();

                await client.FlushAsync();
                client.Stop();

                Assert.Equal(1_000, context.Counter);
            }
        }

        [Fact]
        public async Task SendMessageOneWay_FromHost_1000x()
        {
            using (var duplexer = new Duplexer())
            using (var host = new MessageTransceiver(registry))
            using (var client = new MessageTransceiver(registry)) {
                var context = new MessageContext();
                host.Context = context;
                client.Context = context;

                host.Start(duplexer.StreamA);
                client.Start(duplexer.StreamB);

                var waitTask = new TaskCompletionSource<object>();

                var _client = client;
                var _ = Task.Run(() => {
                    for (var i = 0; i < 1000; i++)
                        _client.SendOneWay(new TestMessage());

                    _client.Flush();
                    _client.Stop();

                    waitTask.SetResult(null);
                });

                await waitTask.Task;

                await host.FlushAsync();
                host.Stop();

                Assert.Equal(1_000, context.Counter);
            }
        }

        private class MessageContext
        {
            public int Counter;
        }

        private class TestMessage : IRequestMessage
        {
            public string MessageId {get; set;}
        }

        private class TestMessageProcessor : MessageProcessorBase<TestMessage>
        {
            public override async Task<IResponseMessage> Process(TestMessage requestMessage)
            {
                var context = (MessageContext)Transceiver.Context;
                Interlocked.Increment(ref context.Counter);

                return await Task.FromResult<IResponseMessage>(null);
            }
        }
    }
}
