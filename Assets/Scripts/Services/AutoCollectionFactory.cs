using Zenject;

namespace SwiftRiver.Services
{
    public class AutoCollectionFactory
    {
        private readonly DiContainer _container;

        public AutoCollectionFactory(DiContainer container)
        {
            _container = container;
        }

        public IAutoCollectionService CreateAlwaysActive()
        {
            return _container.Instantiate<AlwaysActiveAutoCollectionService>();
        }

        public IAutoCollectionService CreateTabDependent()
        {
            return _container.Instantiate<TabDependentAutoCollectionService>();
        }
    }
}
