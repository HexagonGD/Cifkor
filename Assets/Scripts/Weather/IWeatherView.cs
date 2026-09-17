namespace SwiftRiver.Weather
{
    public interface IWeatherView
    {
        void Show();
        void Hide();
        void UpdateWeather(string icon, string temperature);
        void ShowLoading();
        void HideLoading();
        void ShowError(string message);
    }
}
