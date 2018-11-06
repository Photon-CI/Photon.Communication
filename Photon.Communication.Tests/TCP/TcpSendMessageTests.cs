﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Photon.Messaging.Messages;
using Xunit;

namespace Photon.Messaging.Tests.TCP
{
    [TestFixture, IntegrationTest]
    public class TcpSendMessageTests : IClassFixture<T>, IDisposable
    {
        private const int Port = 10933;

        private readonly MessageListener listener;
        private readonly MessageClient client;


        public TcpSendMessageTests()
        {
            var registry = new MessageProcessorRegistry();
            registry.Register(typeof(TestMessageProcessor));
            registry.Register(typeof(TestMessageOneWayProcessor));

            listener = new MessageListener(registry);
            client = new MessageClient(registry);
        }

        public void Dispose()
        {
            client?.Dispose();
            listener?.Dispose();
        }

        [OneTimeSetUp]
        []
        public async Task Begin()
        {
            listener.Listen(IPAddress.Any, Port);

            await client.ConnectAsync("localhost", Port, CancellationToken.None);
        }

        [OneTimeTearDown]
        public void End()
        {
            client.Disconnect();
            listener.Stop();
        }

        [Test]
        public async Task SendMessageOneWay()
        {
            var completeEvent = new TaskCompletionSource<bool>();
            TestMessageOneWayProcessor._event = completeEvent;

            var message = new TestRequestOneWayMessage();
            client.SendOneWay(message);

            var result = await completeEvent.Task;
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task SendMessageResponse()
        {
            var request = new TestRequestMessage {
                Value = 2,
            };

            var response = await client.Send(request)
                .GetResponseAsync<TestResponseMessage>();

            Assert.That(response.Value, Is.EqualTo(4));
        }

        [Fact]
        public async Task Send_1000_MessageResponses()
        {
            var timer = Stopwatch.StartNew();

            var responseList = new List<Task<TestResponseMessage>>();

            for (var i = 0; i < 1000; i++) {
                var request = new TestRequestMessage {
                    Value = 2,
                };

                responseList.Add(client.Send(request)
                    .GetResponseAsync<TestResponseMessage>());
            }

            await Task.WhenAll(responseList);

            foreach (var responseTask in responseList) {
                Assert.Equal(4, responseTask.Result.Value);
            }

            timer.Stop();

            var count = responseList.Count;
            output.WriteLine($"Sent {count:N0} request/response messages in {timer.Elapsed}.");
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
                return await Task.FromResult(new TestResponseMessage {
                    RequestMessageId = requestMessage.MessageId,
                    Value = requestMessage.Value * 2,
                });
            }
        }

        private class TestMessageOneWayProcessor : MessageProcessorBase<TestRequestOneWayMessage>
        {
            internal static TaskCompletionSource<bool> _event;

            
            public override async Task<IResponseMessage> Process(TestRequestOneWayMessage requestMessage)
            {
                _event?.SetResult(true);

                return await Task.FromResult((IResponseMessage)null);
            }
        }
    }
}
