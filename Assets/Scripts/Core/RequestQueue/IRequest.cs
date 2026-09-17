using System.Threading;
using Cysharp.Threading.Tasks;

namespace SwiftRiver.Core.RequestQueue
{
    public interface IRequest
    {
        string Id { get; }
        bool IsCancelled { get; }
        void Cancel();
        UniTask<RequestResult> Execute(CancellationToken cancellationToken = default);
        void OnComplete(RequestResult result);
    }
}
