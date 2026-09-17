using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using SwiftRiver.Services;
using UnityEngine;

namespace SwiftRiver.Core.RequestQueue
{
    public class HttpRequest : IRequest, IDisposable
    {
        public string Id { get; }
        public bool IsCancelled { get; private set; }

        private readonly string _url;
        private readonly HttpMethod _method;
        private readonly Action<string> _onSuccess;
        private readonly Action<string> _onError;
        private readonly IHttpClient _httpClient;
        private readonly int _timeoutSeconds;
        private CancellationTokenSource _cts;
        private bool _disposed;
        private bool _completed;

        public HttpRequest(
            string id,
            string url,
            HttpMethod method,
            Action<string> onSuccess,
            Action<string> onError,
            IHttpClient httpClient,
            int timeoutSeconds = 10)
        {
            Id = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString() : id;
            _url = url;
            _method = method;
            _onSuccess = onSuccess;
            _onError = onError;
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _timeoutSeconds = Mathf.Clamp(timeoutSeconds <= 0 ? 10 : timeoutSeconds, 1, 120);
        }

        public void Cancel()
        {
            IsCancelled = true;
            _cts?.Cancel();
        }

        public async UniTask<RequestResult> Execute(CancellationToken cancellationToken = default)
        {
            if (IsCancelled || cancellationToken.IsCancellationRequested)
            {
                var early = RequestResult.Cancelled;
                SafeComplete(early);
                return early;
            }

            if (string.IsNullOrWhiteSpace(_url))
            {
                _onError?.Invoke("URL is empty");
                var invalid = RequestResult.Failure;
                SafeComplete(invalid);
                return invalid;
            }

            _cts?.Dispose();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            RequestResult result;

            try
            {
                var response = await _httpClient.SendRequest(new HttpRequestOptions
                {
                    RequestId = Id,
                    Url = _url,
                    Method = _method,
                    TimeoutSeconds = _timeoutSeconds,
                }, _cts.Token);

                if (IsCancelled || _cts.IsCancellationRequested || cancellationToken.IsCancellationRequested)
                {
                    result = RequestResult.Cancelled;
                }
                else if (response.Success)
                {
                    _onSuccess?.Invoke(response.Body ?? string.Empty);
                    result = RequestResult.Success;
                }
                else if (response.IsCancelled)
                {
                    result = RequestResult.Cancelled;
                }
                else if (response.IsTimeout)
                {
                    _onError?.Invoke(response.Error);
                    result = RequestResult.Timeout;
                }
                else
                {
                    _onError?.Invoke(response.Error ?? "Request failed");
                    result = RequestResult.Failure;
                }
            }
            catch (OperationCanceledException)
            {
                result = RequestResult.Cancelled;
            }
            catch (Exception ex)
            {
                _onError?.Invoke(ex.Message);
                Debug.LogException(ex);
                result = RequestResult.Failure;
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;
            }

            SafeComplete(result);
            return result;
        }

        public void OnComplete(RequestResult result)
        {
        }

        private void SafeComplete(RequestResult result)
        {
            if (_completed)
                return;
            _completed = true;
            OnComplete(result);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }
}
