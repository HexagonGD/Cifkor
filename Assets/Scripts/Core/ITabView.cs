namespace SwiftRiver.Core
{
    public interface ITabView
    {
        void Show();
        void Hide();
        void OnTabActivated();
        void OnTabDeactivated();
    }
}
