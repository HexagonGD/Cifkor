using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using SwiftRiver.Core;
using SwiftRiver.Core.Popup;
using SwiftRiver.Core.RequestQueue;
using SwiftRiver.Models.DogBreeds;
using SwiftRiver.Services;
using UniRx;
using UnityEngine;
using Zenject;

namespace SwiftRiver.DogBreeds
{
    public class DogBreedsPresenter : IInitializable, IDisposable, ITabView
    {
        private readonly IDogBreedsView _view;
        private readonly DogBreedsConfig _config;
        private readonly IRequestQueue _requestQueue;
        private readonly IHttpClient _httpClient;
        private readonly IPopupView _popupView;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        private CancellationTokenSource _lifetimeCts;
        private string _lastRequestId;
        private List<BreedInfo> _breeds = new List<BreedInfo>();
        private bool _disposed;

        public DogBreedsPresenter(
            IDogBreedsView view,
            DogBreedsConfig config,
            IRequestQueue requestQueue,
            IHttpClient httpClient,
            IPopupView popupView)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _requestQueue = requestQueue ?? throw new ArgumentNullException(nameof(requestQueue));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _popupView = popupView ?? throw new ArgumentNullException(nameof(popupView));
        }

        public void Initialize()
        {
            _view.BreedSelected
                .Subscribe(index => FetchBreedInfoAsync(index, GetLifetimeToken()).Forget(HandleForget))
                .AddTo(_disposables);
        }

        private CancellationToken GetLifetimeToken()
        {
            if (_lifetimeCts == null || _lifetimeCts.IsCancellationRequested)
            {
                _lifetimeCts?.Dispose();
                _lifetimeCts = new CancellationTokenSource();
            }
            return _lifetimeCts.Token;
        }

        private static void HandleForget(Exception ex)
        {
            if (!(ex is OperationCanceledException))
                Debug.LogException(ex);
        }

        public void Show() => _view.Show();
        public void Hide() => _view.Hide();

        public void OnTabActivated()
        {
            _view.Show();
            GetLifetimeToken();
            FetchBreedsAsync(GetLifetimeToken()).Forget(HandleForget);
        }

        public void OnTabDeactivated()
        {
            CancelLastRequest();
            _lifetimeCts?.Cancel();
            _view.HideAllBreedLoading();
            _view.HideLoading();
            _view.Hide();
        }

        private void CancelLastRequest()
        {
            if (string.IsNullOrEmpty(_lastRequestId))
                return;
            _requestQueue.CancelById(_lastRequestId);
            _lastRequestId = null;
        }

        private async UniTask FetchBreedsAsync(CancellationToken lifetimeToken)
        {
            lifetimeToken.ThrowIfCancellationRequested();
            _view.ShowLoading();

            string currentRequestId = Guid.NewGuid().ToString();
            _lastRequestId = currentRequestId;

            var completion = new UniTaskCompletionSource<bool>();
            string body = null;
            string error = null;

            var request = new HttpRequest(
                currentRequestId,
                $"{_config.ApiBaseUrl.TrimEnd('/')}/breeds",
                HttpMethod.Get,
                b => { body = b; completion.TrySetResult(true); },
                e => { error = e; completion.TrySetResult(false); },
                _httpClient,
                _config.RequestTimeoutSeconds);

            try
            {
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(lifetimeToken))
                {
                    var enqueueTask = _requestQueue.EnqueueAsync(request, linkedCts.Token);
                    bool ok = await completion.Task.AttachExternalCancellation(linkedCts.Token);
                    RequestResult queueResult = await enqueueTask.AttachExternalCancellation(linkedCts.Token);

                    if (linkedCts.Token.IsCancellationRequested)
                        throw new OperationCanceledException();

                    if (ok && queueResult == RequestResult.Success && !string.IsNullOrEmpty(body))
                    {
                        _breeds = ParseBreedsResponse(body);
                        _view.UpdateBreedList(_breeds);
                    }
                    else if (queueResult == RequestResult.Cancelled)
                    {
                        throw new OperationCanceledException();
                    }
                    else
                    {
                        _view.ShowError(string.IsNullOrEmpty(error) ? "Failed to load breeds" : error);
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
                _view.ShowError("Failed to load breeds");
            }
            finally
            {
                if (_lastRequestId == currentRequestId)
                    _lastRequestId = null;
                _view.HideLoading();
            }
        }

        public async UniTask FetchBreedInfoAsync(int breedIndex, CancellationToken lifetimeToken = default)
        {
            if (_breeds == null || breedIndex < 0 || breedIndex >= _breeds.Count)
                return;

            var breed = _breeds[breedIndex];
            if (breed == null || string.IsNullOrEmpty(breed.id))
                return;

            CancelLastRequest();
            _view.HideAllBreedLoading();

            string currentRequestId = Guid.NewGuid().ToString();
            _lastRequestId = currentRequestId;
            _view.ShowBreedLoading(breedIndex);

            var completion = new UniTaskCompletionSource<bool>();
            string body = null;
            string error = null;

            var request = new HttpRequest(
                currentRequestId,
                $"{_config.ApiBaseUrl.TrimEnd('/')}/breeds/{breed.id}",
                HttpMethod.Get,
                b => { body = b; completion.TrySetResult(true); },
                e => { error = e; completion.TrySetResult(false); },
                _httpClient,
                _config.RequestTimeoutSeconds);

            try
            {
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(lifetimeToken))
                {
                    var enqueueTask = _requestQueue.EnqueueAsync(request, linkedCts.Token);
                    bool ok = await completion.Task.AttachExternalCancellation(linkedCts.Token);
                    RequestResult queueResult = await enqueueTask.AttachExternalCancellation(linkedCts.Token);

                    if (linkedCts.Token.IsCancellationRequested)
                        throw new OperationCanceledException();

                    if (ok && queueResult == RequestResult.Success && !string.IsNullOrEmpty(body))
                    {
                        var breedInfo = ParseSingleBreedResponse(body) ?? breed;
                        ShowBreedPopup(breedInfo);
                    }
                    else if (queueResult != RequestResult.Cancelled)
                    {
                        Debug.LogError($"{nameof(DogBreedsPresenter)} Detail request failed: {error}");
                        ShowBreedPopup(breed);
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
            }
            finally
            {
                if (_lastRequestId == currentRequestId)
                    _lastRequestId = null;
                _view.HideBreedLoading(breedIndex);
            }
        }

        internal List<BreedInfo> ParseBreedsResponse(string json)
        {
            try
            {
                try
                {
                    var envelope = JsonConvert.DeserializeObject<DogBreedsListResponse>(json);
                    if (envelope?.Data != null && envelope.Data.Count > 0)
                    {
                        var mapped = new List<BreedInfo>(envelope.Data.Count);
                        foreach (var item in envelope.Data)
                        {
                            if (item?.Attributes == null)
                                continue;
                            var info = BreedInfo.FromAttributes(item.Id, item.Attributes);
                            if (info != null)
                                mapped.Add(info);
                        }
                        if (mapped.Count > 0)
                            return ClampCount(mapped);
                    }
                }
                catch { }

                var flat = JsonConvert.DeserializeObject<List<BreedInfo>>(json);
                return ClampCount(flat ?? new List<BreedInfo>());
            }
            catch (Exception ex)
            {
                Debug.LogError($"{nameof(DogBreedsPresenter)} Parse error: {ex.Message}");
                return new List<BreedInfo>();
            }
        }

        internal BreedInfo ParseSingleBreedResponse(string json)
        {
            try
            {
                try
                {
                    var single = JsonConvert.DeserializeObject<DogBreedSingleResponse>(json);
                    if (single?.Data?.Attributes != null)
                        return BreedInfo.FromAttributes(single.Data.Id, single.Data.Attributes);
                }
                catch { }

                return JsonConvert.DeserializeObject<BreedInfo>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"{nameof(DogBreedsPresenter)} Parse error: {ex.Message}");
                return null;
            }
        }

        private List<BreedInfo> ClampCount(List<BreedInfo> breeds)
        {
            int count = Mathf.Clamp(_config.BreedsCount, 1, 100);
            if (breeds.Count > count)
                return breeds.GetRange(0, count);
            return breeds;
        }

        private void ShowBreedPopup(BreedInfo breed)
        {
            if (breed == null)
                return;
            string name = string.IsNullOrEmpty(breed.name) ? "Unknown breed" : breed.name;
            string content = $"Description: {breed.description}\n\n";
            content += $"Life Span: {breed.lifeSpan}\n\n";
            if (!string.IsNullOrEmpty(breed.temperament))
                content += $"Temperament: {breed.temperament}\n\n";
            if (!string.IsNullOrEmpty(breed.origin))
                content += $"Origin: {breed.origin}\n\n";
            if (!string.IsNullOrEmpty(breed.heightMetric))
                content += $"Height: {breed.heightMetric} cm\n\n";
            if (!string.IsNullOrEmpty(breed.weightMetric))
                content += $"Weight: {breed.weightMetric} kg";

            float adaptiveHeight = Mathf.Clamp(content.Length * 1.5f, _config.PopupMinHeight, _config.PopupMaxHeight);
            _popupView.Show(name, content, adaptiveHeight);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _disposables?.Dispose();
            _lifetimeCts?.Cancel();
            _lifetimeCts?.Dispose();
            CancelLastRequest();
        }
    }
}
