using System;
using SwiftRiver.Models.Clicker;
using UniRx;
using UnityEngine;
using Zenject;

namespace SwiftRiver.Services
{
    public class AlwaysActiveAutoCollectionService : IAutoCollectionService, IInitializable, ITickable, IDisposable
    {
        private readonly ClickerConfig _clickerConfig;
        private readonly Subject<Unit> _triggered = new Subject<Unit>();
        private float _timer;
        private bool _started;

        public AlwaysActiveAutoCollectionService(ClickerConfig clickerConfig)
        {
            _clickerConfig = clickerConfig != null
                ? clickerConfig
                : throw new ArgumentNullException(nameof(clickerConfig));
        }

        public IObservable<Unit> CollectionTriggered => _triggered;

        public void Initialize()
        {
            _started = true;
            _timer = 0f;
        }

        public void StartCollecting()
        {
            _started = true;
        }

        public void StopCollecting()
        {
        }

        public void Tick()
        {
            if (!_started)
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
