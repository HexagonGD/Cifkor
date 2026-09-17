using UnityEngine;

namespace SwiftRiver.Models.Weather
{
    [CreateAssetMenu(fileName = "WeatherConfig", menuName = "SwiftRiver/WeatherConfig")]
    public class WeatherConfig : ScriptableObject
    {
        [Header("API Settings")]
        [SerializeField] private string _apiUrl = "https://api.weather.gov/gridpoints/TOP/32,81/forecast";
        [Min(1f)] [SerializeField] private float _pollingInterval = 30f;
        [Min(1)] [SerializeField] private int _requestTimeoutSeconds = 10;

        public string ApiUrl => _apiUrl;
        public float PollingInterval => _pollingInterval;
        public int RequestTimeoutSeconds => _requestTimeoutSeconds;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_apiUrl))
                _apiUrl = "https://api.weather.gov/gridpoints/TOP/32,81/forecast";
            _pollingInterval = Mathf.Max(5f, _pollingInterval);
            _requestTimeoutSeconds = Mathf.Clamp(_requestTimeoutSeconds, 1, 60);
        }
    }
}
