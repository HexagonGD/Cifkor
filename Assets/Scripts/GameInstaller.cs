using SwiftRiver.Clicker;
using SwiftRiver.Core;
using SwiftRiver.Core.NavigationBar;
using SwiftRiver.Core.Popup;
using SwiftRiver.Core.RequestQueue;
using SwiftRiver.DogBreeds;
using SwiftRiver.Models;
using SwiftRiver.Models.Clicker;
using SwiftRiver.Models.DogBreeds;
using SwiftRiver.Models.Weather;
using SwiftRiver.Services;
using SwiftRiver.Weather;
using UnityEngine;
using Zenject;

namespace SwiftRiver
{
    public class GameInstaller : MonoInstaller
    {
        [Header("Configs")]
        [SerializeField] private ClickerConfig _clickerConfig;
        [SerializeField] private EnergyConfig _energyConfig;
        [SerializeField] private WeatherConfig _weatherConfig;
        [SerializeField] private DogBreedsConfig _dogBreedsConfig;

        [Header("Views")]
        [SerializeField] private ClickerView _clickerView;
        [SerializeField] private WeatherView _weatherView;
        [SerializeField] private DogBreedsView _dogBreedsView;
        [SerializeField] private NavigationBarView _navigationBarView;
        [SerializeField] private PopupView _popupView;
        
        public override void InstallBindings()
        {
            BindConfigs();
            BindModels();
            BindServices();
            BindViews();
            BindPresenters();
        }

        private void BindConfigs()
        {
            BindConfig(_clickerConfig);
            BindConfig(_energyConfig);
            BindConfig(_weatherConfig);
            BindConfig(_dogBreedsConfig);
        }

        private void BindModels()
        {
            Container.BindInterfacesAndSelfTo<CurrencyModel>().AsSingle().NonLazy();
        }

        private void BindServices()
        {
            Container.BindInterfacesAndSelfTo<TabManager>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<RequestQueue>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<HttpClientService>().AsSingle().NonLazy();

            Container.BindInterfacesAndSelfTo<EnergyService>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<TabDependentAutoCollectionService>().AsSingle().NonLazy();
            Container.Bind<AlwaysActiveAutoCollectionService>().AsTransient();
            Container.Bind<AutoCollectionFactory>().AsSingle();
        }

        private void BindViews()
        {
            BindView<IWeatherView, WeatherView>(_weatherView);
            BindView<IDogBreedsView, DogBreedsView>(_dogBreedsView);
            BindView<IClickerView, ClickerView>(_clickerView);
            BindView<INavigationBarView, NavigationBarView>(_navigationBarView);
            BindView<IPopupView, PopupView>(_popupView);
        }

        private void BindPresenters()
        {
            Container.BindInterfacesAndSelfTo<ClickerPresenter>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<WeatherPresenter>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<DogBreedsPresenter>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<DogPopupPresenter>().AsSingle().NonLazy();

            Container.BindInterfacesAndSelfTo<AppBootstrap>().AsSingle().NonLazy();
        }

        private void BindConfig<T>(T config) where T : ScriptableObject
        {
            if (config == null)
            {
                Debug.LogError($"{nameof(GameInstaller)} {typeof(T).Name} is not assigned. Assign it in the inspector.");
                config = ScriptableObject.CreateInstance<T>();
            }

            Container.Bind<T>().FromInstance(config).AsSingle();
        }

        private void BindView<TInterface, TConcrete>(TConcrete inspectorInstance)
            where TConcrete : Component, TInterface
        {
            if (inspectorInstance != null)
            {
                Container.Bind<TConcrete>().FromInstance(inspectorInstance).AsSingle();
            }
            else
            {
                Debug.LogWarning($"{nameof(GameInstaller)} {typeof(TConcrete).Name} not assigned, resolving from scene.");
                Container.Bind<TConcrete>().FromComponentInHierarchy().AsSingle();
            }

            Container.Bind<TInterface>().To<TConcrete>().FromResolve();
        }
    }
}
