using System;
using UniRx;

namespace SwiftRiver.Models
{
    public class CurrencyModel : IDisposable
    {
        private readonly ReactiveProperty<int> _balance = new ReactiveProperty<int>(0);

        public int Balance => _balance.Value;
        public IReadOnlyReactiveProperty<int> BalanceReactive => _balance;

        public void Add(int amount)
        {
            if (amount <= 0)
                return;
            _balance.Value += amount;
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0)
                return false;
            if (_balance.Value < amount)
                return false;
            _balance.Value -= amount;
            return true;
        }

        public void Set(int amount)
        {
            if (amount < 0)
                amount = 0;
            _balance.Value = amount;
        }

        public void Dispose()
        {
            _balance?.Dispose();
        }
    }
}
