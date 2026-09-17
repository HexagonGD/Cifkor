using System;
using DG.Tweening;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace SwiftRiver.Core.Popup
{
    public class PopupView : MonoBehaviour, IPopupView
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _contentText;
        [SerializeField] private Button _closeButton;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _contentRoot;

        private readonly Subject<Unit> _closeRequested = new Subject<Unit>();
        private Tween _showTween;

        public IObservable<Unit> OnClose => _closeRequested;

        private void Awake()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(RequestClose);
            else
                Debug.LogError($"{nameof(PopupView)} Close button not assigned.", this);

            if (_contentRoot == null)
            {
                var fitter = GetComponentInChildren<ContentSizeFitter>(true);
                if (fitter != null)
                    _contentRoot = (RectTransform)fitter.transform;
                else
                    Debug.LogError($"{nameof(PopupView)} Content root not assigned.", this);
            }
        }

        private void RequestClose()
        {
            _closeRequested.OnNext(Unit.Default);
        }

        public void Show(string title, string content, float adaptiveHeight)
        {
            if (_titleText == null || _contentText == null)
            {
                Debug.LogError($"{nameof(PopupView)} Title/Content text not assigned.", this);
                return;
            }
            _titleText.text = title ?? string.Empty;
            _contentText.text = content ?? string.Empty;

            RebuildLayout();
            PlayShowAnimation();
        }

        public void Hide()
        {
            _showTween?.Kill();
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
        }

        private void RebuildLayout()
        {
            if (_contentRoot != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRoot);
        }

        private void PlayShowAnimation()
        {
            _showTween?.Kill();
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
                _showTween = _canvasGroup.DOFade(1f, 0.2f).SetUpdate(true);
            }
        }

        private void OnDestroy()
        {
            _showTween?.Kill();
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(RequestClose);
            _closeRequested?.OnCompleted();
            _closeRequested?.Dispose();
        }
    }
}
