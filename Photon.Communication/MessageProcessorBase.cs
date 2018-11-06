using System.Threading.Tasks;
using Photon.Communication.Messages;

namespace Photon.Communication
{
    public abstract class MessageProcessorBase<TRequest, TResponse> : IProcessMessage<TRequest, TResponse>
        where TRequest : IRequestMessage
        where TResponse : IResponseMessage
    {
        public MessageTransceiver Transceiver {get; set;}


        public abstract Task<TResponse> Process(TRequest requestMessage);
    }

    public abstract class MessageProcessorBase<T> : IProcessMessage<T>
        where T : IRequestMessage
    {
        public MessageTransceiver Transceiver {get; set;}


        public abstract Task<IResponseMessage> Process(T requestMessage);
    }
}
