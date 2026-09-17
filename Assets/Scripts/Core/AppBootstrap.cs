using System;
using SwiftRiver.Clicker;
using SwiftRiver.Core.NavigationBar;
using SwiftRiver.DogBreeds;
using SwiftRiver.Weather;
using UniRx;
using UnityEngine;
using Zenject;

namespace SwiftRiver.Core
{
    public class AppBootstrap : IInitializable, IDisposable
    {
        private readonly ITabManager _tabManager;
        private readonly ClickerPresenter _clicker;
        private readonly WeatherPresenter _weather;
        private readonly DogBreedsPresenter _dogBreeds;
        private readonly INavigationBarView _navigationBar;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private bool _disposed;

        public AppBootstrap(
            ITabManager tabManager,
            ClickerPresenter clicker,
            WeatherPresenter weather,
            DogBreedsPresenter dogBreeds,
            INavigationBarView navigationBar)
        {
            _tabManager = tabManager ?? throw new ArgumentNullException(nameof(tabManager));
            _clicker = clicker ?? throw new ArgumentNullException(nameof(clicker));
            _weather = weather ?? throw new ArgumentNullException(nameof(weather));
            _dogBreeds = dogBreeds ?? throw new ArgumentNullException(nameof(dogBreeds));
            _navigationBar = navigationBar ?? throw new ArgumentNullException(nameof(navigationBar));
        }

        public void Initialize()
        {
            _tabManager.RegisterTab(_clicker);
            _tabManager.RegisterTab(_weather);
            _tabManager.RegisterTab(_dogBreeds);

            _navigationBar.OnTabSelected
                .Subscribe(index =>
                {
                    _tabManager.SwitchToTab(index);
                    _navigationBar.SetActiveTab(_tabManager.ActiveTabIndex);
                })
                .AddTo(_disposables);

            _tabManager.ActiveTabIndexReactive
                .Subscribe(_navigationBar.SetActiveTab)
                .AddTo(_disposables);

            _tabManager.SwitchToTab(0);
            _navigationBar.SetActiveTab(_tabManager.ActiveTabIndex);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _disposables?.Dispose();
        }
    }
}