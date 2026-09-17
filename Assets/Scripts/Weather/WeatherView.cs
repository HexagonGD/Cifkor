using TMPro;
using UnityEngine;

namespace SwiftRiver.Weather
{
    public class WeatherView : MonoBehaviour, IWeatherView
    {
        [SerializeField] private TextMeshProUGUI _weatherIcon;
        [SerializeField] private TextMeshProUGUI _weatherText;
        [SerializeField] private GameObject _loadingIndicator;
        [SerializeField] private TextMeshProUGUI _errorText;

        private void Awake()
        {
            if (_loadingIndicator != null)
                _loadingIndicator.SetActive(false);
            if (_errorText != null)
            {
                _errorText.gameObject.SetActive(false);
                _errorText.text = string.Empty;
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void UpdateWeather(string icon, string temperature)
        {
            if (_weatherIcon != null)
                _weatherIcon.text = string.IsNullOrEmpty(icon) ? "🌤" : icon;

            if (_weatherText != null)
                _weatherText.text = $"Today - {temperature}";

            if (_errorText != null)
            {
                _errorText.text = string.Empty;
                _errorText.gameObject.SetActive(false);
            }
        }

        public void ShowLoading()
        {
            if (_loadingIndicator != null)
                _loadingIndicator.SetActive(true);
        }

        public void HideLoading()
        {
            if (_loadingIndicator != null)
                _loadingIndicator.SetActive(false);
        }

        public void ShowError(string message)
        {
            HideLoading();
            if (_errorText != null)
            {
                _errorText.text = message ?? "Failed to load weather";
                _errorText.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"{nameof(WeatherView)} {message}");
            }
        }
    }
}
