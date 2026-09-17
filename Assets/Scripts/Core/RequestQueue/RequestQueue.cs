using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SwiftRiver.Core.RequestQueue
{
    public interface IRequestQueue
    {
        void Enqueue(IRequest request);
        UniTask<RequestResult> EnqueueAsync(IRequest request, CancellationToken cancellationToken = default);
        void CancelById(string id);
    }

    public class RequestQueue : IRequestQueue, IDisposable
    {
        private class QueueEntry
        {
            public IRequest Request;
            public UniTaskCompletionSource<RequestResult> Completion;
            public CancellationToken CancellationToken;
        }

        private readonly Queue<QueueEntry> _queue = new Queue<QueueEntry>();
        private QueueEntry _current;
        private bool _isProcessing;
        private bool _disposed;

        public void Enqueue(IRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            EnqueueAsync(request).Forget(ex => Debug.LogException(ex));
        }

        public async UniTask<RequestResult> EnqueueAsync(IRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (_disposed)
                return RequestResult.Cancelled;

            var entry = new QueueEntry
            {
                Request = request,
                Completion = new UniTaskCompletionSource<RequestResult>(),
                CancellationToken = cancellationToken,
            };

            bool alreadyCancelled = cancellationToken.IsCancellationRequested || request.IsCancelled;
            lock (_queue)
            {
                if (_disposed)
                {
                    entry.Completion.TrySetResult(RequestResult.Cancelled);
                    return RequestResult.Cancelled;
                }
                _queue.Enqueue(entry);
            }

            if (alreadyCancelled)
            {
                CancelById(request.Id);
            }

            if (!_isProcessing)
                ProcessNextAsync().Forget(ex => Debug.LogException(ex));

            return await entry.Completion.Task;
        }

        public void CancelById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return;

            List<QueueEntry> toCancel = null;
            lock (_queue)
            {
                if (_current != null && _current.Request != null && _current.Request.Id == id)
                {
                    _current.Request.Cancel();
                }

                if (_queue.Count > 0)
                {
                    var kept = new Queue<QueueEntry>(_queue.Count);
                    toCancel = new List<QueueEntry>();
                    while (_queue.Count > 0)
                    {
                        var entry = _queue.Dequeue();
                        if (entry.Request != null && entry.Request.Id == id)
                            toCancel.Add(entry);
                        else
                            kept.Enqueue(entry);
                    }
                    while (kept.Count > 0)
                        _queue.Enqueue(kept.Dequeue());
                }
            }

            if (toCancel == null)
                return;
            foreach (var entry in toCancel)
            {
                entry.Request?.Cancel();
                entry.Request?.OnComplete(RequestResult.Cancelled);
                (entry.Request as IDisposable)?.Dispose();
                entry.Completion.TrySetResult(RequestResult.Cancelled);
            }
        }

        private async UniTask ProcessNextAsync()
        {
            if (_isProcessing)
                return;
            _isProcessing = true;

            try
            {
                while (true)
                {
                    QueueEntry entry;
                    lock (_queue)
                    {
                        if (_disposed || _queue.Count == 0)
                        {
                            _current = null;
                            _isProcessing = false;
                            return;
                        }
                        entry = _queue.Dequeue();
                        _current = entry;
                    }

                    RequestResult result;
                    try
                    {
                        if (entry.CancellationToken.IsCancellationRequested || entry.Request.IsCancelled)
                        {
                            entry.Request.Cancel();
                            result = RequestResult.Cancelled;
                        }
                        else
                        {
                            result = await entry.Request.Execute(entry.CancellationToken);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        result = RequestResult.Cancelled;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                        result = RequestResult.Failure;
                    }

                    entry.Request?.OnComplete(result);
                    (entry.Request as IDisposable)?.Dispose();
                    entry.Completion.TrySetResult(result);

                    lock (_queue)
                    {
                        if (ReferenceEquals(_current, entry))
                            _current = null;
                    }
                }
            }
            finally
            {
                _isProcessing = false;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            List<QueueEntry> pending;
            lock (_queue)
            {
                pending = new List<QueueEntry>(_queue);
                _queue.Clear();
            }
            _current?.Request?.Cancel();
            foreach (var entry in pending)
            {
                entry.Request?.Cancel();
                entry.Request?.OnComplete(RequestResult.Cancelled);
                (entry.Request as IDisposable)?.Dispose();
                entry.Completion.TrySetResult(RequestResult.Cancelled);
            }
        }
    }
}
