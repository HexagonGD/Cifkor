using System;
using UniRx;

namespace SwiftRiver.Core.NavigationBar
{
    public interface INavigationBarView
    {
        IObservable<int> OnTabSelected { get; }

        void SetActiveTab(int tabIndex);
    }
}
