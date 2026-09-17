using System;
using SwiftRiver.Models.Clicker;
using UniRx;
using UnityEngine;
using Zenject;

namespace SwiftRiver.Services
{
    public class EnergyService : IEnergyService, IInitializable, ITickable, IDisposable
    {
        private readonly EnergyConfig _energyConfig;
        private readonly ReactiveProperty<int> _energy = new ReactiveProperty<int>(0);
        private float _regenTimer;
        private bool _initialized;

        public EnergyService(EnergyConfig energyConfig)
        {
            _energyConfig = energyConfig != null
                ? energyConfig
                : throw new ArgumentNullException(nameof(energyConfig));
        }

        public int CurrentEnergy => _energy.Value;
        public int MaxEnergy => _energyConfig.MaxEnergy;
        public IReadOnlyReactiveProperty<int> EnergyReactive => _energy;

        public void Initialize()
        {
            if (_initialized)
                return;
            _initialized = true;
            _energy.Value = _energyConfig.MaxEnergy;
            _regenTimer = 0f;
        }

        public bool TrySpend(int amount)
        {
            if (!_initialized || amount <= 0)
                return false;
            if (_energy.Value < amount)
                return false;
            _energy.Value -= amount;
            return true;
        }

        public void Restore()
        {
            if (!_initialized)
                return;
            _regenTimer = 0f;
            RestoreTick(999f);
        }

        public void Tick()
        {
            if (!_initialized)
                return;
            if (_energy.Value >= _energyConfig.MaxEnergy)
            {
                _regenTimer = 0f;
                return;
            }

            float interval = Mathf.Max(0.1f, _energyConfig.EnergyRestoreInterval);
            _regenTimer += Time.deltaTime;
            if (_regenTimer < interval)
                return;

            _regenTimer = 0f;
            RestoreTick(interval);
        }

        private void RestoreTick(float _)
        {
            int room = _energyConfig.MaxEnergy - _energy.Value;
            if (room <= 0)
                return;
            int amount = Mathf.Clamp(_energyConfig.EnergyRestoreAmount, 1, room);
            _energy.Value += amount;
        }

        public void Dispose()
        {
            _energy?.Dispose();
        }
    }
}
