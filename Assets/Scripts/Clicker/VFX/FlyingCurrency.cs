using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DG.Tweening;

namespace SwiftRiver.Clicker.VFX
{
    public class FlyingCurrency : IFlyingCurrency, IDisposable
    {
        private readonly RectTransform _rectTransform;
        private Sequence _sequence;
        private bool _disposed;

        public FlyingCurrency(RectTransform rectTransform)
        {
            _rectTransform = rectTransform != null
                ? rectTransform
                : throw new ArgumentNullException(nameof(rectTransform));
        }

        public async UniTask Animate(Vector3 startPosition, Vector3 endPosition, CancellationToken cancellationToken = default)
        {
            if (_disposed || _rectTransform == null)
                return;

            KillSequence();

            _rectTransform.anchoredPosition = startPosition;

            var completion = new UniTaskCompletionSource<bool>();
            using (cancellationToken.Register(() =>
            {
                KillSequence();
                completion.TrySetResult(false);
            }))
            {
                _sequence = DOTween.Sequence();
                _sequence.Append(_rectTransform.DOAnchorPos(endPosition, 1f).SetEase(Ease.OutCubic));
                _sequence.AppendCallback(() => completion.TrySetResult(true));
                _sequence.Play();

                await completion.Task;
            }
        }

        public void Destroy()
        {
            KillSequence();
            if (_rectTransform != null)
            {
                UnityEngine.Object.Destroy(_rectTransform.gameObject);
            }
        }

        private void KillSequence()
        {
            if (_sequence != null)
            {
                if (_sequence.IsActive())
                    _sequence.Kill();
                _sequence = null;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            KillSequence();
        }
    }
}
