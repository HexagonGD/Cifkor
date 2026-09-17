using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SwiftRiver.Clicker.VFX
{
    public class ParticleEffect : IParticleEffect, IDisposable
    {
        private const int MaxWaitSeconds = 5;

        private readonly ParticleSystem _template;
        private bool _disposed;

        public ParticleEffect(ParticleSystem template)
        {
            _template = template != null
                ? template
                : throw new ArgumentNullException(nameof(template));
        }

        public async UniTask Spawn(Vector3 position, CancellationToken cancellationToken = default)
        {
            if (_disposed || _template == null)
                return;

            ParticleSystem instance = UnityEngine.Object.Instantiate(_template, position, Quaternion.identity);

            if (instance == null)
                return;

            instance.Play();

            await DestroyAfterFinishAsync(instance, cancellationToken);
        }

        public void Destroy()
        {
            Dispose();
        }

        private static async UniTask DestroyAfterFinishAsync(ParticleSystem instance, CancellationToken cancellationToken)
        {
            float elapsed = 0f;
            try
            {
                while (instance != null && instance.IsAlive(true))
                {
                    if (cancellationToken.IsCancellationRequested || elapsed >= MaxWaitSeconds)
                        break;
                    elapsed += Time.deltaTime;
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }
            }
            finally
            {
                SafeDestroy(instance);
            }
        }

        private static void SafeDestroy(ParticleSystem instance)
        {
            if (instance == null)
                return;
            if (instance.gameObject != null)
                UnityEngine.Object.Destroy(instance.gameObject);
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
