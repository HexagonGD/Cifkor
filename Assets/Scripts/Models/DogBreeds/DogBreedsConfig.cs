using UnityEngine;

namespace SwiftRiver.Models.DogBreeds
{
    [CreateAssetMenu(fileName = "DogBreedsConfig", menuName = "SwiftRiver/DogBreedsConfig")]
    public class DogBreedsConfig : ScriptableObject
    {
        [Header("API Settings")]
        [SerializeField] private string _apiBaseUrl = "https://dogapi.dog/api/v2";
        [Range(1, 100)] [SerializeField] private int _breedsCount = 10;
        [Min(1)] [SerializeField] private int _requestTimeoutSeconds = 10;
        [SerializeField] private float _popupMinHeight = 300f;
        [SerializeField] private float _popupMaxHeight = 800f;

        public string ApiBaseUrl => _apiBaseUrl;
        public int BreedsCount => _breedsCount;
        public int RequestTimeoutSeconds => _requestTimeoutSeconds;
        public float PopupMinHeight => _popupMinHeight;
        public float PopupMaxHeight => _popupMaxHeight;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_apiBaseUrl))
                _apiBaseUrl = "https://dogapi.dog/api/v2";
            _apiBaseUrl = _apiBaseUrl.TrimEnd('/');
            _breedsCount = Mathf.Clamp(_breedsCount, 1, 100);
            _requestTimeoutSeconds = Mathf.Clamp(_requestTimeoutSeconds, 1, 60);
            _popupMinHeight = Mathf.Max(100f, _popupMinHeight);
            _popupMaxHeight = Mathf.Max(_popupMinHeight, _popupMaxHeight);
        }
    }
}
