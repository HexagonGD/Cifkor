using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace SwiftRiver.Services
{
    public interface IHttpClient
    {
        UniTask<HttpResponse> SendRequest(string requestId, string url, HttpMethod method, CancellationToken cancellationToken = default);
        UniTask<HttpResponse> SendRequest(HttpRequestOptions options, CancellationToken cancellationToken = default);
        void CancelRequest(string requestId);
    }

    public struct HttpRequestOptions
    {
        public string RequestId;
        public string Url;
        public HttpMethod Method;
        public Dictionary<string, string> Headers;
        public string Body;
        public string ContentType;
        public int TimeoutSeconds;
    }

    public struct HttpResponse
    {
        public bool Success;
        public string Body;
        public string Error;
        public long StatusCode;
        public bool IsTimeout;
        public bool IsCancelled;
    }

    public enum HttpMethod
    {
        Get,
        Post
    }
}
