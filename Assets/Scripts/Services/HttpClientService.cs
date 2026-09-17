using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace SwiftRiver.Services
{
    public class HttpClientService : IHttpClient, IDisposable
    {
        private const string DefaultUserAgent = "SwiftRiver-Unity/1.0 (UnityWebRequest)";
        private const int DefaultTimeoutSeconds = 10;

        private readonly Dictionary<string, UnityWebRequest> _activeRequests = new Dictionary<string, UnityWebRequest>();
        private readonly object _gate = new object();
        private bool _disposed;

        public UniTask<HttpResponse> SendRequest(string requestId, string url, HttpMethod method, CancellationToken cancellationToken = default)
        {
            return SendRequest(new HttpRequestOptions
            {
                RequestId = requestId,
                Url = url,
                Method = method,
                TimeoutSeconds = DefaultTimeoutSeconds,
            }, cancellationToken);
        }

        public async UniTask<HttpResponse> SendRequest(HttpRequestOptions options, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                return new HttpResponse { Success = false, Error = "HttpClient disposed" };

            if (string.IsNullOrWhiteSpace(options.Url))
                return new HttpResponse { Success = false, Error = "URL is empty" };

            if (!Uri.TryCreate(options.Url, UriKind.Absolute, out _))
                return new HttpResponse { Success = false, Error = $"Invalid URL: {options.Url}" };

            string requestId = string.IsNullOrEmpty(options.RequestId) ? Guid.NewGuid().ToString() : options.RequestId;
            int timeout = Mathf.Clamp(options.TimeoutSeconds <= 0 ? DefaultTimeoutSeconds : options.TimeoutSeconds, 1, 120);

            UnityWebRequest request = CreateRequest(options, timeout);
            if (request == null)
                return new HttpResponse { Success = false, Error = "Failed to create request" };

            lock (_gate)
            {
                _activeRequests[requestId] = request;
            }

            try
            {
                using (var timeoutCts = new CancellationTokenSource())
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token))
                {
                    timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeout));

                    var operation = request.SendWebRequest();

                    while (!operation.isDone)
                    {
                        if (linkedCts.Token.IsCancellationRequested)
                        {
                            request.Abort();
                            linkedCts.Token.ThrowIfCancellationRequested();
                        }
                        await UniTask.Yield(PlayerLoopTiming.Update, linkedCts.Token);
                    }

                    bool cancelled = cancellationToken.IsCancellationRequested;
                    if (cancelled)
                    {
                        return new HttpResponse { Success = false, Error = "Cancelled", IsCancelled = true, StatusCode = request.responseCode };
                    }

                    if (timeoutCts.IsCancellationRequested && request.result != UnityWebRequest.Result.Success)
                    {
                        return new HttpResponse
                        {
                            Success = false,
                            Error = $"Timeout after {timeout}s: {request.error}",
                            StatusCode = request.responseCode,
                            IsTimeout = true,
                        };
                    }

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string body = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                        return new HttpResponse { Success = true, Body = body ?? string.Empty, Error = string.Empty, StatusCode = request.responseCode };
                    }

                    bool isTimeout = request.error != null && request.error.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0;
                    return new HttpResponse
                    {
                        Success = false,
                        Error = $"{request.error} (HTTP {(int)request.responseCode})",
                        StatusCode = request.responseCode,
                        IsTimeout = isTimeout,
                    };
                }
            }
            catch (OperationCanceledException)
            {
                bool callerCancelled = cancellationToken.IsCancellationRequested;
                return new HttpResponse
                {
                    Success = false,
                    Error = callerCancelled ? "Cancelled" : $"Timeout after {timeout}s",
                    StatusCode = request.responseCode,
                    IsCancelled = callerCancelled,
                    IsTimeout = !callerCancelled,
                };
            }
            catch (Exception ex)
            {
                return new HttpResponse { Success = false, Error = ex.Message, StatusCode = request.responseCode };
            }
            finally
            {
                lock (_gate)
                {
                    if (_activeRequests.TryGetValue(requestId, out var current) && ReferenceEquals(current, request))
                        _activeRequests.Remove(requestId);
                }
                request.Dispose();
            }
        }

        public void CancelRequest(string requestId)
        {
            if (string.IsNullOrEmpty(requestId))
                return;
            UnityWebRequest request = null;
            lock (_gate)
            {
                _activeRequests.TryGetValue(requestId, out request);
            }
            if (request == null)
                return;
            request.Abort();
        }

        private static UnityWebRequest CreateRequest(HttpRequestOptions options, int timeout)
        {
            UnityWebRequest request;
            switch (options.Method)
            {
                case HttpMethod.Post:
                    request = new UnityWebRequest(options.Url, "POST");
                    if (!string.IsNullOrEmpty(options.Body))
                    {
                        byte[] bodyRaw = Encoding.UTF8.GetBytes(options.Body);
                        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    }
                    request.downloadHandler = new DownloadHandlerBuffer();
                    break;
                case HttpMethod.Get:
                default:
                    request = new UnityWebRequest(options.Url, "GET");
                    request.downloadHandler = new DownloadHandlerBuffer();
                    break;
            }

            request.timeout = timeout;

            bool hasUserAgent = false;
            if (options.Headers != null)
            {
                foreach (var kv in options.Headers)
                {
                    if (string.IsNullOrEmpty(kv.Key))
                        continue;
                    request.SetRequestHeader(kv.Key, kv.Value ?? string.Empty);
                    if (string.Equals(kv.Key, "User-Agent", StringComparison.OrdinalIgnoreCase))
                        hasUserAgent = true;
                }
            }

            if (!hasUserAgent)
            {
                request.SetRequestHeader("User-Agent", DefaultUserAgent);
            }

            if (options.Method == HttpMethod.Post)
            {
                string contentType = string.IsNullOrEmpty(options.ContentType) ? "application/json" : options.ContentType;
                request.SetRequestHeader("Content-Type", contentType);
            }

            return request;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            List<UnityWebRequest> copy;
            lock (_gate)
            {
                copy = new List<UnityWebRequest>(_activeRequests.Values);
                _activeRequests.Clear();
            }
            foreach (var req in copy)
            {
                req.Abort();
                req.Dispose();
            }
        }
    }
}
