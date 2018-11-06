using System.Threading.Tasks;

namespace Photon.Communication.Messages
{
    public interface IProcessMessage
    {
        MessageTransceiver Transceiver {get; set;}
    }

    public interface IProcessMessage<in TRequest, TResponse> : IProcessMessage
        where TRequest : IRequestMessage
        where TResponse : IResponseMessage
    {
        Task<TResponse> Process(TRequest requestMessage);
    }

    public interface IProcessMessage<in TRequest> : IProcessMessage<TRequest, IResponseMessage>
        where TRequest : IRequestMessage {}
}
