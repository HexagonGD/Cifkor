using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SwiftRiver.Clicker.VFX
{
    public interface IParticleEffect
    {
        UniTask Spawn(Vector3 position, CancellationToken cancellationToken = default);
        void Destroy();
    }
}
