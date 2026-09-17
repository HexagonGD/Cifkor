using UniRx;

namespace SwiftRiver.Services
{
    public interface IEnergyService
    {
        int CurrentEnergy { get; }
        int MaxEnergy { get; }
        IReadOnlyReactiveProperty<int> EnergyReactive { get; }

        bool TrySpend(int amount);
        void Initialize();
    }
}
