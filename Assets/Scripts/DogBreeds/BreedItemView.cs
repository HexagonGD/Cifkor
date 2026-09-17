using System;
using SwiftRiver.Models.DogBreeds;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SwiftRiver.DogBreeds
{
    [RequireComponent(typeof(Button))]
    public class BreedItemView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private Button _button;
        [SerializeField] private GameObject _loadingIndicator;

        private int _index = -1;
        private Action<int> _onClicked;
        private bool _isLoading;

        public bool IsLoading => _isLoading;

        private void Awake()
        {
            if (_button == null)
                _button = GetComponent<Button>();

            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleClick);
                _button.onClick.AddListener(HandleClick);
            }
            else
            {
                Debug.LogError($"{nameof(BreedItemView)} Button reference is missing.", this);
            }

            if (_nameText == null)
                Debug.LogError($"{nameof(BreedItemView)} Name text reference is missing. Assign it in the inspector.", this);

            if (_loadingIndicator != null)
                _loadingIndicator.SetActive(false);
        }

        public void Setup(BreedInfo breed, int index, Action<int> onClicked)
        {
            _index = index;
            _onClicked = onClicked;

            if (_nameText != null)
                _nameText.text = breed != null ? $"{index + 1} - {breed.name}" : $"{index + 1} - Unknown";

            HideLoading();
            gameObject.SetActive(true);
        }

        public void ShowLoading()
        {
            _isLoading = true;
            if (_loadingIndicator != null)
                _loadingIndicator.SetActive(true);
        }

        public void HideLoading()
        {
            _isLoading = false;
            if (_loadingIndicator != null)
                _loadingIndicator.SetActive(false);
        }

        private void HandleClick()
        {
            _onClicked?.Invoke(_index);
        }

        private void OnDestroy()
        {
            _onClicked = null;
            if (_button != null)
                _button.onClick.RemoveListener(HandleClick);
        }

#if UNITY_EDITOR
        private void Reset()
        {
            _button = GetComponent<Button>();
            if (_nameText == null)
                _nameText = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        private void OnValidate()
        {
            if (_button == null)
                _button = GetComponent<Button>();
        }
#endif
    }
}