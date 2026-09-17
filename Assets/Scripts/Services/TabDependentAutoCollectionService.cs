using System;
using SwiftRiver.Models.Clicker;
using UniRx;
using UnityEngine;
using Zenject;

namespace SwiftRiver.Services
{
    public class TabDependentAutoCollectionService : IAutoCollectionService, ITickable, IDisposable
    {
        private readonly ClickerConfig _clickerConfig;
        private readonly Subject<Unit> _triggered = new Subject<Unit>();
        private float _timer;
        private bool _collecting;

        public TabDependentAutoCollectionService(ClickerConfig clickerConfig)
        {
            _clickerConfig = clickerConfig != null
                ? clickerConfig
                : throw new ArgumentNullException(nameof(clickerConfig));
        }

        public IObservable<Unit> CollectionTriggered => _triggered;

        public void StartCollecting()
        {
            _collecting = true;
            _timer = 0f;
        }

        public void StopCollecting()
        {
            _collecting = false;
            _timer = 0f;
        }

        public void Tick()
        {
            if (!_collecting)
                return;
            float interval = _clickerConfig != null ? Mathf.Max(0.1f, _clickerConfig.AutoCollectionInterval) : 1f;
            _timer += Time.deltaTime;
            if (_timer < interval)
                return;
            _timer = 0f;
            _triggered.OnNext(Unit.Default);
        }

        public void Dispose()
        {
            _triggered?.OnCompleted();
            _triggered?.Dispose();
        }
    }
}
