using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SwiftRiver.Clicker.VFX
{
    public interface IFlyingCurrency
    {
        UniTask Animate(Vector3 startPosition, Vector3 endPosition, CancellationToken cancellationToken = default);
        void Destroy();
    }
}
