using System;
using UniRx;

namespace SwiftRiver.Core.Popup
{
    public interface IPopupView
    {
        void Show(string title, string content, float adaptiveHeight);
        void Hide();
        IObservable<Unit> OnClose { get; }
    }
}
