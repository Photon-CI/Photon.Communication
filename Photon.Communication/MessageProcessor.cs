using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Photon.Communication.Messages;

namespace Photon.Communication
{
    internal class MessageProcessor
    {
        private readonly MessageProcessorRegistry registry;
        private readonly MessageTransceiver transceiver;

        private ActionBlock<MessageProcessorHandle> queue;

        public object Context {get; set;}


        public MessageProcessor(MessageTransceiver transceiver, MessageProcessorRegistry registry)
        {
            this.transceiver = transceiver ?? throw new ArgumentNullException(nameof(transceiver));
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public void Start()
        {
            queue = new ActionBlock<MessageProcessorHandle>(OnProcess);
        }

        public void Flush(CancellationToken cancellationToken = default)
        {
            queue.Complete();
            queue.Completion.Wait(cancellationToken);
        }

        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            queue.Complete();

            await Task.Run(async () => {
                await queue.Completion;
            }, cancellationToken);
        }

        public MessageProcessorHandle Process(IRequestMessage requestMessage)
        {
            var handle = new MessageProcessorHandle(requestMessage);
            queue.Post(handle);
            return handle;
        }

        private async Task OnProcess(MessageProcessorHandle handle)
        {
            try {
                var messageType = handle.RequestMessage.GetType();
                if (!registry.TryGet(messageType, out var processFunc))
                    throw new ApplicationException($"No Message Processor was found matching message type '{messageType.Name}'!");

                var result = await processFunc(transceiver, handle.RequestMessage);

                handle.SetResult(result);
            }
            catch (Exception error) {
                handle.SetException(error);
            }
        }
    }
}
