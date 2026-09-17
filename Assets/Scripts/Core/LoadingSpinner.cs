using DG.Tweening;
using UnityEngine;

namespace SwiftRiver.Core
{
    [RequireComponent(typeof(RectTransform))]
    public class LoadingSpinner : MonoBehaviour
    {
        [SerializeField] private RectTransform _target;
        [SerializeField] private float _degreesPerSecond = 360f;

        private Tween _spinTween;

        private void Awake()
        {
            if (_target == null)
                _target = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            StartSpinning();
        }

        private void OnDisable()
        {
            StopSpinning();
        }

        private void OnDestroy()
        {
            StopSpinning();
        }

        private void StartSpinning()
        {
            if (_target == null || _degreesPerSecond <= 0f)
                return;

            StopSpinning();

            float duration = 360f / _degreesPerSecond;
            _spinTween = _target
                .DORotate(new Vector3(0f, 0f, -360f), duration, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental)
                .SetUpdate(true);
        }

        private void StopSpinning()
        {
            if (_spinTween != null)
            {
                if (_spinTween.IsActive())
                    _spinTween.Kill();
                _spinTween = null;
            }
        }

#if UNITY_EDITOR
        private void Reset()
        {
            if (_target == null)
                _target = GetComponent<RectTransform>();
        }

        private void OnValidate()
        {
            if (_target == null)
                _target = GetComponent<RectTransform>();
        }
#endif
    }
}
