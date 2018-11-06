using Photon.Communication.Messages;
using Photon.Communication.Tests.Internal.TRx;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Photon.Communication.Tests
{
    public class SendMessageTests
    {
        private readonly ITestOutputHelper output;
        private readonly MessageProcessorRegistry registry;


        public SendMessageTests(ITestOutputHelper output)
        {
            this.output = output;

            registry = new MessageProcessorRegistry();
            registry.Register(typeof(TestMessageProcessor));
            registry.Register(typeof(TestMessageOneWayProcessor));
        }

        [Fact]
        public async Task SendMessageOneWay()
        {
            var context = new MessageCompleteContext();

            using (var duplexer = new Duplexer())
            using (var transceiverA = new MessageTransceiver(registry))
            using (var transceiverB = new MessageTransceiver(registry)) {
                transceiverA.Context = context;
                transceiverB.Context = context;

                transceiverA.Start(duplexer.StreamA);
                transceiverB.Start(duplexer.StreamB);

                var message = new TestRequestOneWayMessage();
                transceiverA.SendOneWay(message);

                await context.CompleteEvent.Task;
            }
        }

        [Fact]
        public async Task SendMessageResponse()
        {
            using (var duplexer = new Duplexer())
            using (var transceiverA = new MessageTransceiver(registry))
            using (var transceiverB = new MessageTransceiver(registry)) {
                transceiverA.Start(duplexer.StreamA);
                transceiverB.Start(duplexer.StreamB);

                var request = new TestRequestMessage {
                    Value = 2,
                };

                var response = await transceiverA.Send(request)
                    .GetResponseAsync<TestResponseMessage>();

                Assert.Equal(4, response.Value);
            }
        }

        [Fact]
        public async Task Send_1000_MessageResponses()
        {
            var timer = Stopwatch.StartNew();
            const int count = 1_000;

            using (var duplexer = new Duplexer())
            using (var transceiverA = new MessageTransceiver(registry))
            using (var transceiverB = new MessageTransceiver(registry)) {
                transceiverA.Start(duplexer.StreamA);
                transceiverB.Start(duplexer.StreamB);

                for (var i = 0; i < count; i++) {
                    var request = new TestRequestMessage {
                        Value = 2,
                    };

                    var response = await transceiverA.Send(request)
                        .GetResponseAsync<TestResponseMessage>();

                    Assert.Equal(4, response.Value);
                }
            }

            timer.Stop();

            output.WriteLine($"Sent {count:N0} request/response messages in {timer.Elapsed}.");
        }

        [Fact]
        public async Task Send_Delayed_MessageResponses()
        {
            var timer = Stopwatch.StartNew();
            const int count = 3;

            using (var duplexer = new Duplexer())
            using (var transceiverA = new MessageTransceiver(registry))
            using (var transceiverB = new MessageTransceiver(registry)) {
                transceiverA.Start(duplexer.StreamA);
                transceiverB.Start(duplexer.StreamB);

                for (var i = 0; i < count; i++) {
                    var request = new TestRequestMessage {
                        Value = 2,
                    };

                    await Task.Delay(3_000);

                    var response = await transceiverA.Send(request)
                        .GetResponseAsync<TestResponseMessage>();

                    Assert.Equal(4, response.Value);
                }
            }

            timer.Stop();

            output.WriteLine($"Sent {count:N0} request/response messages in {timer.Elapsed}.");
        }

        private class MessageCompleteContext
        {
            public TaskCompletionSource<object> CompleteEvent {get;} = new TaskCompletionSource<object>();
        }

        private class TestRequestOneWayMessage : IRequestMessage
        {
            public string MessageId {get; set;}
        }

        private class TestRequestMessage : IRequestMessage
        {
            public string MessageId {get; set;}
            public int Value {get; set;}
        }

        private class TestResponseMessage : ResponseMessageBase
        {
            public int Value {get; set;}
        }

        private class TestMessageProcessor : MessageProcessorBase<TestRequestMessage>
        {
            public override async Task<IResponseMessage> Process(TestRequestMessage requestMessage)
            {
                return new TestResponseMessage {
                    RequestMessageId = requestMessage.MessageId,
                    Value = requestMessage.Value * 2,
                };
            }
        }

        private class TestMessageOneWayProcessor : MessageProcessorBase<TestRequestOneWayMessage>
        {
            public override Task<IResponseMessage> Process(TestRequestOneWayMessage requestMessage)
            {
                var context = (MessageCompleteContext)Transceiver.Context;
                context.CompleteEvent.SetResult(true);

                return Task.FromResult<IResponseMessage>(null);
            }
        }
    }
}
