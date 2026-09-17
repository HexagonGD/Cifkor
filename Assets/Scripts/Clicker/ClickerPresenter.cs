using System;
using SwiftRiver.Core;
using SwiftRiver.Models;
using SwiftRiver.Models.Clicker;
using SwiftRiver.Services;
using UniRx;
using UnityEngine;
using Zenject;

namespace SwiftRiver.Clicker
{
    public class ClickerPresenter : IInitializable, IDisposable, ITabView
    {
        private readonly IClickerView _view;
        private readonly CurrencyModel _currencyModel;
        private readonly IEnergyService _energyService;
        private readonly IAutoCollectionService _autoCollectionService;
        private readonly ClickerConfig _clickerConfig;
        private readonly EnergyConfig _energyConfig;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private bool _bound;

        public ClickerPresenter(
            IClickerView view,
            CurrencyModel currencyModel,
            IEnergyService energyService,
            IAutoCollectionService autoCollectionService,
            ClickerConfig clickerConfig,
            EnergyConfig energyConfig)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _currencyModel = currencyModel ?? throw new ArgumentNullException(nameof(currencyModel));
            _energyService = energyService ?? throw new ArgumentNullException(nameof(energyService));
            _autoCollectionService = autoCollectionService ?? throw new ArgumentNullException(nameof(autoCollectionService));
            _clickerConfig = clickerConfig ?? throw new ArgumentNullException(nameof(clickerConfig));
            _energyConfig = energyConfig ?? throw new ArgumentNullException(nameof(energyConfig));
        }

        public void Initialize()
        {
            Bind();
        }

        public void Bind()
        {
            if (_bound)
                return;
            _bound = true;

            _view.Tapped
                .Subscribe(_ => OnTap())
                .AddTo(_disposables);

            _currencyModel.BalanceReactive
                .Subscribe(_view.UpdateBalance)
                .AddTo(_disposables);

            _energyService.EnergyReactive
                .Subscribe(energy => _view.UpdateEnergy(energy, _energyService.MaxEnergy))
                .AddTo(_disposables);

            _autoCollectionService.CollectionTriggered
                .Subscribe(_ => OnAutoCollection())
                .AddTo(_disposables);

            _view.UpdateBalance(_currencyModel.Balance);
            _view.UpdateEnergy(_energyService.CurrentEnergy, _energyService.MaxEnergy);
        }

        public void Unbind()
        {
            if (!_bound)
                return;
            _bound = false;
            _disposables.Clear();
        }

        private void OnTap()
        {
            int cost = Mathf.Max(0, _energyConfig.EnergyCostPerTap);
            if (!_energyService.TrySpend(cost))
                return;
            int reward = Mathf.Max(1, _clickerConfig.CurrencyPerTap);
            _currencyModel.Add(reward);
            _view.PlayTapEffects(reward);
        }

        private void OnAutoCollection()
        {
            int cost = Mathf.Max(0, _energyConfig.EnergyCostPerAuto);
            if (!_energyService.TrySpend(cost))
                return;
            int reward = Mathf.Max(1, _clickerConfig.CurrencyPerAuto);
            _currencyModel.Add(reward);
            _view.PlayTapEffects(reward);
        }

        public void Show() => _view.Show();
        public void Hide() => _view.Hide();

        public void OnTabActivated()
        {
            _view.Show();
            _autoCollectionService.StartCollecting();
        }

        public void OnTabDeactivated()
        {
            _autoCollectionService.StopCollecting();
            _view.Hide();
        }

        public void Dispose()
        {
            Unbind();
            _disposables?.Dispose();
        }
    }
}
