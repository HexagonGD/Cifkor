using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using SwiftRiver.Core;
using SwiftRiver.Core.RequestQueue;
using SwiftRiver.Models.Weather;
using SwiftRiver.Services;
using UnityEngine;
using Zenject;

namespace SwiftRiver.Weather
{
    public class WeatherPresenter : IInitializable, IDisposable, ITabView
    {
        private readonly IWeatherView _view;
        private readonly WeatherConfig _weatherConfig;
        private readonly IRequestQueue _requestQueue;
        private readonly IHttpClient _httpClient;

        private CancellationTokenSource _pollingCts;
        private string _lastRequestId;
        private bool _disposed;

        public WeatherPresenter(
            IWeatherView view,
            WeatherConfig weatherConfig,
            IRequestQueue requestQueue,
            IHttpClient httpClient)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _weatherConfig = weatherConfig ?? throw new ArgumentNullException(nameof(weatherConfig));
            _requestQueue = requestQueue ?? throw new ArgumentNullException(nameof(requestQueue));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public void Initialize()
        {
        }

        public void Show() => _view.Show();
        public void Hide() => _view.Hide();

        public void OnTabActivated()
        {
            _view.Show();
            StopPolling();
            _pollingCts = new CancellationTokenSource();
            StartPolling(_pollingCts.Token).Forget(ex =>
            {
                if (!(ex is OperationCanceledException))
                    Debug.LogException(ex);
            });
        }

        public void OnTabDeactivated()
        {
            StopPolling();
            CancelLastRequest();
            _view.HideLoading();
            _view.Hide();
        }

        private void StopPolling()
        {
            var cts = Interlocked.Exchange(ref _pollingCts, null);
            if (cts == null)
                return;
            cts.Cancel();
            cts.Dispose();
        }

        private void CancelLastRequest()
        {
            if (string.IsNullOrEmpty(_lastRequestId))
                return;
            _requestQueue.CancelById(_lastRequestId);
            _lastRequestId = null;
        }

        private async UniTask StartPolling(CancellationToken token)
        {
            float interval = Mathf.Max(5f, _weatherConfig.PollingInterval);
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await FetchWeather(token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }

                try
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(interval), cancellationToken: token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async UniTask FetchWeather(CancellationToken pollingToken)
        {
            pollingToken.ThrowIfCancellationRequested();

            _view.ShowLoading();

            string currentRequestId = Guid.NewGuid().ToString();
            _lastRequestId = currentRequestId;

            var completion = new UniTaskCompletionSource<bool>();
            string body = null;
            string error = null;

            var request = new HttpRequest(
                currentRequestId,
                _weatherConfig.ApiUrl,
                HttpMethod.Get,
                b => { body = b; completion.TrySetResult(true); },
                e => { error = e; completion.TrySetResult(false); },
                _httpClient,
                _weatherConfig.RequestTimeoutSeconds);

            try
            {
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(pollingToken))
                {
                    var enqueueTask = _requestQueue.EnqueueAsync(request, linkedCts.Token);
                    bool completed = await completion.Task.AttachExternalCancellation(linkedCts.Token);

                    RequestResult queueResult = await enqueueTask.AttachExternalCancellation(linkedCts.Token);

                    if (linkedCts.Token.IsCancellationRequested)
                        throw new OperationCanceledException();

                    if (completed && queueResult == RequestResult.Success && !string.IsNullOrEmpty(body))
                    {
                        ParseWeatherResponse(body);
                    }
                    else if (queueResult == RequestResult.Cancelled)
                    {
                        throw new OperationCanceledException();
                    }
                    else
                    {
                        _view.ShowError(string.IsNullOrEmpty(error) ? "Failed to load weather" : error);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                _view.ShowError("Failed to load weather");
            }
            finally
            {
                if (_lastRequestId == currentRequestId)
                    _lastRequestId = null;
                _view.HideLoading();
            }
        }

        private void ParseWeatherResponse(string json)
        {
            try
            {
                var response = JsonConvert.DeserializeObject<WeatherResponse>(json);
                var periods = response?.Properties?.Periods;
                if (periods == null || periods.Length == 0)
                {
                    _view.ShowError("No weather data");
                    return;
                }

                var period = periods[0];
                string unit = string.IsNullOrEmpty(period.TemperatureUnit) ? "F" : period.TemperatureUnit;
                string temperature = $"{period.Temperature}°{unit}";
                string icon = string.IsNullOrEmpty(period.Icon) ? "🌤" : GetWeatherEmoji(period.Icon);
                _view.UpdateWeather(icon, temperature);
            }
            catch (Exception ex)
            {
                Debug.LogError($"{nameof(WeatherPresenter)} Parse error: {ex.Message}");
                _view.ShowError("Failed to parse weather");
            }
        }

        internal static string GetWeatherEmoji(string iconUrl)
        {
            if (string.IsNullOrEmpty(iconUrl))
                return "🌤";
            string lower = iconUrl.ToLowerInvariant();

            if (lower.Contains("/skc") || lower.Contains("/few")) return "☀️";
            if (lower.Contains("/sct") || lower.Contains("/bkn")) return "🌤";
            if (lower.Contains("/ovc")) return "☁️";
            if (lower.Contains("rain") || lower.Contains("shra") || lower.Contains("tsra") || lower.Contains("/ra")) return "🌧️";
            if (lower.Contains("snow") || lower.Contains("/sn")) return "❄️";
            if (lower.Contains("fog") || lower.Contains("/fg")) return "🌫️";
            if (lower.Contains("wind") || lower.Contains("/wind")) return "💨";

            if (lower.Contains("cloudy")) return "☁️";
            if (lower.Contains("sunny") || lower.Contains("clear")) return "☀️";
            return "🌤";
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            StopPolling();
            CancelLastRequest();
        }
    }
}
