using System;
using UniRx;

namespace SwiftRiver.Clicker
{
    public interface IClickerView
    {
        IObservable<Unit> Tapped { get; }

        void Show();
        void Hide();
        void UpdateBalance(int balance);
        void UpdateEnergy(int currentEnergy, int maxEnergy);
        void AnimateButtonPress();
        void PlayTapEffects(int amount);
    }
}
