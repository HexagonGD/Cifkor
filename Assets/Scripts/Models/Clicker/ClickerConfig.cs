using UnityEngine;

namespace SwiftRiver.Models.Clicker
{
    [CreateAssetMenu(fileName = "ClickerConfig", menuName = "SwiftRiver/ClickerConfig")]
    public class ClickerConfig : ScriptableObject
    {
        [Header("Currency")]
        [Min(1)] [SerializeField] private int _currencyPerTap = 1;
        [Min(1)] [SerializeField] private int _currencyPerAuto = 1;

        [Header("Auto Collection")]
        [Min(0.1f)] [SerializeField] private float _autoCollectionInterval = 3f;

        public int CurrencyPerTap => _currencyPerTap;
        public int CurrencyPerAuto => _currencyPerAuto;
        public float AutoCollectionInterval => _autoCollectionInterval;

        private void OnValidate()
        {
            _currencyPerTap = Mathf.Max(1, _currencyPerTap);
            _currencyPerAuto = Mathf.Max(1, _currencyPerAuto);
            _autoCollectionInterval = Mathf.Max(0.1f, _autoCollectionInterval);
        }
    }
}
