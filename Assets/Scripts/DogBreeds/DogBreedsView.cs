using System;
using System.Collections.Generic;
using SwiftRiver.Models.DogBreeds;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace SwiftRiver.DogBreeds
{
    public class DogBreedsView : MonoBehaviour, IDogBreedsView
    {
        [SerializeField] private ScrollRect _breedListScrollRect;
        [SerializeField] private Transform _breedListContent;
        [SerializeField] private BreedItemView _breedItemPrefab;
        [SerializeField] private GameObject _loadingIndicator;
        [SerializeField] private TextMeshProUGUI _errorText;

        private List<BreedInfo> _breeds = new List<BreedInfo>();
        private readonly List<BreedItemView> _spawnedItems = new List<BreedItemView>();
        private readonly Subject<int> _breedSelected = new Subject<int>();

        public IObservable<int> BreedSelected => _breedSelected;

        private void Awake()
        {
            if (_loadingIndicator != null)
                _loadingIndicator.SetActive(false);
            if (_errorText != null)
            {
                _errorText.text = string.Empty;
                _errorText.gameObject.SetActive(false);
            }
            if (_breedItemPrefab == null)
                Debug.LogError($"{nameof(DogBreedsView)} Breed item prefab not assigned.", this);
            if (_breedListContent == null)
                Debug.LogError($"{nameof(DogBreedsView)} Breed list content not assigned.", this);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void UpdateBreedList(List<BreedInfo> breeds)
        {
            _breeds = breeds ?? new List<BreedInfo>();
            RefreshBreedList();
            HideError();
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

        public void ShowBreedLoading(int index)
        {
            if (index < 0 || index >= _spawnedItems.Count)
                return;
            var item = _spawnedItems[index];
            if (item != null)
                item.ShowLoading();
        }

        public void HideBreedLoading(int index)
        {
            if (index < 0 || index >= _spawnedItems.Count)
                return;
            var item = _spawnedItems[index];
            if (item != null)
                item.HideLoading();
        }

        public void HideAllBreedLoading()
        {
            for (int i = 0; i < _spawnedItems.Count; i++)
            {
                var item = _spawnedItems[i];
                if (item != null)
                    item.HideLoading();
            }
        }

        public void ShowError(string message)
        {
            HideLoading();
            if (_errorText != null)
            {
                _errorText.text = message ?? "Failed to load breeds";
                _errorText.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"{nameof(DogBreedsView)} {message}");
            }
        }

        private void HideError()
        {
            if (_errorText != null)
            {
                _errorText.text = string.Empty;
                _errorText.gameObject.SetActive(false);
            }
        }

        private void RefreshBreedList()
        {
            ClearSpawnedItems();
            if (_breedListContent == null || _breedItemPrefab == null)
                return;

            for (int i = 0; i < _breeds.Count; i++)
            {
                var breed = _breeds[i];
                if (breed == null)
                    continue;

                BreedItemView item = Instantiate(_breedItemPrefab, _breedListContent);

                if (item == null)
                {
                    Debug.LogWarning($"{nameof(DogBreedsView)} Spawned breed prefab is null at index {i}.", this);
                    continue;
                }

                item.Setup(breed, i, NotifySelected);
                _spawnedItems.Add(item);
            }
        }

        private void NotifySelected(int index)
        {
            _breedSelected.OnNext(index);
        }

        private void ClearSpawnedItems()
        {
            _spawnedItems.Clear();

            if (_breedListContent == null)
                return;
            for (int i = _breedListContent.childCount - 1; i >= 0; i--)
            {
                var child = _breedListContent.GetChild(i);
                if (child == null)
                    continue;

                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }

        private void OnDestroy()
        {
            ClearSpawnedItems();
            _breedSelected?.OnCompleted();
            _breedSelected?.Dispose();
        }
    }
}
