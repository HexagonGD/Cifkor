using System;
using SwiftRiver.Core.Popup;
using UniRx;
using UnityEngine;
using Zenject;

namespace SwiftRiver.DogBreeds
{
    public class DogPopupPresenter : IInitializable, IDisposable
    {
        private readonly IPopupView _popupView;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private bool _disposed;

        public DogPopupPresenter(IPopupView popupView)
        {
            _popupView = popupView ?? throw new ArgumentNullException(nameof(popupView));
        }

        public void Initialize()
        {
            Bind();
        }

        public void Bind()
        {
            _popupView.OnClose
                .Subscribe(_ => HandleClose())
                .AddTo(_disposables);
        }

        public void Unbind()
        {
            _disposables.Clear();
        }

        private void HandleClose()
        {
            _popupView.Hide();
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            Unbind();
            _disposables?.Dispose();
        }
    }
}
