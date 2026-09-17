using System;
using Newtonsoft.Json;

namespace SwiftRiver.Models.Weather
{
    [Serializable]
    public class WeatherResponse
    {
        [JsonProperty("properties")]
        public WeatherProperties Properties;
    }

    [Serializable]
    public class WeatherProperties
    {
        [JsonProperty("periods")]
        public WeatherPeriod[] Periods;
    }

    [Serializable]
    public class WeatherPeriod
    {
        [JsonProperty("name")]
        public string Name;

        [JsonProperty("temperature")]
        public int Temperature;

        [JsonProperty("temperatureUnit")]
        public string TemperatureUnit;

        [JsonProperty("shortForecast")]
        public string ShortForecast;

        [JsonProperty("detailedForecast")]
        public string DetailedForecast;

        [JsonProperty("icon")]
        public string Icon;
    }
}
