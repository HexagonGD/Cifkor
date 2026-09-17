using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace SwiftRiver.Clicker
{
    public class ClickerView : MonoBehaviour, IClickerView
    {
        [SerializeField] private Button _clickButton;
        [SerializeField] private TextMeshProUGUI _balanceText;
        [SerializeField] private TextMeshProUGUI _energyText;
        [SerializeField] private RectTransform _flyingCurrencyPrefab;
        [SerializeField] private ParticleSystem _particleEffect;
        [SerializeField] private Transform _currencySpawnPoint;
        [SerializeField] private RectTransform _flyTarget;

        private const int PoolPrewarm = 8;
        private const int PoolMaxSize = 32;
        private const float FlyDistance = 100f;
        private const float FlyDuration = 1f;
        private const float FlySpawnSpread = 30f;

        private readonly Subject<Unit> _tapSubject = new Subject<Unit>();
        private readonly Stack<RectTransform> _pool = new Stack<RectTransform>();
        private readonly HashSet<RectTransform> _active = new HashSet<RectTransform>();
        private CancellationTokenSource _effectsCts;
        private Tween _buttonTween;
        private Tween _balanceTween;

        public IObservable<Unit> Tapped => _tapSubject;

        private RectTransform FlyTarget =>
            _flyTarget != null
                ? _flyTarget
                : _balanceText != null
                    ? _balanceText.rectTransform
                    : null;

        private CancellationToken EffectsToken
        {
            get
            {
                if (_effectsCts == null || _effectsCts.IsCancellationRequested)
                {
                    _effectsCts?.Dispose();
                    _effectsCts = new CancellationTokenSource();
                }
                return _effectsCts.Token;
            }
        }

        private void Awake()
        {
            if (_clickButton != null)
                _clickButton.onClick.AddListener(HandleClick);
            else
                Debug.LogError($"{nameof(ClickerView)} Click button not assigned.", this);

            if (_balanceText == null || _energyText == null)
                Debug.LogError($"{nameof(ClickerView)} Balance/Energy text not assigned.", this);

            PrewarmPool();
        }

        private void HandleClick()
        {
            _tapSubject.OnNext(Unit.Default);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            _effectsCts?.Cancel();
            _buttonTween?.Kill();
            _balanceTween?.Kill();
            ClearFlyingCurrencies();
            gameObject.SetActive(false);
        }

        public void UpdateBalance(int balance)
        {
            if (_balanceText != null)
                _balanceText.text = $"Balance: {balance}";
        }

        public void UpdateEnergy(int currentEnergy, int maxEnergy)
        {
            if (_energyText != null)
                _energyText.text = $"Energy: {currentEnergy}/{maxEnergy}";
        }

        public void AnimateButtonPress()
        {
            if (_clickButton == null)
                return;
            _buttonTween?.Kill();
            _buttonTween = _clickButton.transform
                .DOPunchScale(Vector3.one * 0.05f, 0.2f, 1, 0.5f)
                .SetUpdate(true);
        }

        public void PlayTapEffects(int amount)
        {
            AnimateButtonPress();
            SpawnFlyingCurrencyInternal(amount, EffectsToken).Forget(ex =>
            {
                if (!(ex is OperationCanceledException))
                    Debug.LogException(ex);
            });
            SpawnParticleInternal(EffectsToken).Forget(ex =>
            {
                if (!(ex is OperationCanceledException))
                    Debug.LogException(ex);
            });
        }

        private async UniTask SpawnFlyingCurrencyInternal(int amount, CancellationToken token)
        {
            if (_flyingCurrencyPrefab == null || _currencySpawnPoint == null)
            {
                Debug.LogWarning($"{nameof(ClickerView)} Flying currency prefab or spawn point missing.", this);
                return;
            }

            RectTransform currency = GetFromPool();
            if (currency == null)
                return;

            try
            {
                var text = currency.GetComponent<TextMeshProUGUI>();
                if (text == null)
                    text = currency.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                    text.text = $"+{amount}";

                RectTransform targetRect = FlyTarget;
                Transform parent = currency.parent;

                Vector2 startAnchored = Vector2.zero + UnityEngine.Random.insideUnitCircle * FlySpawnSpread;
                currency.anchoredPosition = startAnchored;
                Vector2 target = ResolveFlyTarget(startAnchored, targetRect, parent);

                Tween tween = currency.DOAnchorPos(target, FlyDuration)
                    .SetEase(Ease.InCubic)
                    .SetUpdate(true);
                var completion = new UniTaskCompletionSource<bool>();
                tween.OnComplete(() => completion.TrySetResult(true));
                tween.OnKill(() => completion.TrySetResult(false));
                bool reachedTarget;
                using (token.Register(() => completion.TrySetResult(false)))
                {
                    reachedTarget = await completion.Task;
                }
                if (tween.IsActive())
                    tween.Kill();
                if (reachedTarget)
                    PunchFlyTarget(targetRect);
            }
            finally
            {
                ReleaseToPool(currency);
            }
        }

        private Vector2 ResolveFlyTarget(Vector2 startAnchored, RectTransform targetRect, Transform parent)
        {
            if (targetRect == null || parent == null)
                return startAnchored + Vector2.up * FlyDistance;

            Vector3 targetLocal = parent.InverseTransformPoint(targetRect.position);
            return new Vector2(targetLocal.x, targetLocal.y);
        }

        private void PunchFlyTarget(RectTransform targetRect)
        {
            if (targetRect == null)
                return;
            _balanceTween?.Kill();
            _balanceTween = targetRect
                .DOPunchScale(Vector3.one * 0.1f, 0.25f, 1, 0.5f)
                .SetUpdate(true);
        }

        private async UniTask SpawnParticleInternal(CancellationToken token)
        {
            if (_particleEffect == null)
                return;
            
            Transform parent = _currencySpawnPoint != null ? _currencySpawnPoint : transform;
            ParticleSystem instance = Instantiate(_particleEffect, parent, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.Play();

            try
            {
                float elapsed = 0f;
                while (instance != null && instance.IsAlive(true))
                {
                    if (token.IsCancellationRequested || elapsed > 5f)
                        break;
                    elapsed += Time.deltaTime;
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }
            finally
            {
                if (instance != null)
                    Destroy(instance.gameObject);
            }
        }

        private void PrewarmPool()
        {
            if (_flyingCurrencyPrefab == null || _currencySpawnPoint == null)
                return;
            for (int i = 0; i < PoolPrewarm; i++)
            {
                var item = CreatePooledItem();
                if (item != null)
                {
                    item.gameObject.SetActive(false);
                    _pool.Push(item);
                }
            }
        }

        private RectTransform CreatePooledItem()
        {
            var item = Instantiate(_flyingCurrencyPrefab, _currencySpawnPoint);
            item.gameObject.SetActive(false);
            return item;
        }

        private RectTransform GetFromPool()
        {
            RectTransform item = null;
            while (_pool.Count > 0)
            {
                item = _pool.Pop();
                if (item != null)
                    break;
                item = null;
            }
            if (item == null)
                item = CreatePooledItem();
            if (item == null)
                return null;

            item.gameObject.SetActive(true);
            _active.Add(item);
            return item;
        }

        private void ReleaseToPool(RectTransform item)
        {
            if (item == null)
                return;
            if (!_active.Contains(item))
                return;
            _active.Remove(item);
            item.DOKill();
            if (this != null && gameObject != null)
            {
                item.gameObject.SetActive(false);
                if (_pool.Count < PoolMaxSize)
                    _pool.Push(item);
                else
                    Destroy(item.gameObject);
            }
            else
            {
                Destroy(item.gameObject);
            }
        }

        private void ClearFlyingCurrencies()
        {
            foreach (var currency in _active)
            {
                if (currency == null)
                    continue;

                currency.DOKill();
                currency.gameObject.SetActive(false);
                if (_pool.Count < PoolMaxSize)
                    _pool.Push(currency);
                else
                    Destroy(currency.gameObject);
            }
            _active.Clear();
        }

        private void OnDestroy()
        {
            _effectsCts?.Cancel();
            _effectsCts?.Dispose();
            _buttonTween?.Kill();
            _balanceTween?.Kill();
            if (_clickButton != null)
                _clickButton.onClick.RemoveListener(HandleClick);
            _tapSubject?.OnCompleted();
            _tapSubject?.Dispose();
        }
    }
}