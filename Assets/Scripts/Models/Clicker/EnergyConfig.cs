using UnityEngine;

namespace SwiftRiver.Models.Clicker
{
    [CreateAssetMenu(fileName = "EnergyConfig", menuName = "SwiftRiver/EnergyConfig")]
    public class EnergyConfig : ScriptableObject
    {
        [Header("Energy")]
        [Min(1)] [SerializeField] private int _maxEnergy = 1000;
        [Min(0)] [SerializeField] private int _energyCostPerTap = 1;
        [Min(0)] [SerializeField] private int _energyCostPerAuto = 1;
        [Min(1)] [SerializeField] private int _energyRestoreAmount = 10;
        [Min(0.1f)] [SerializeField] private float _energyRestoreInterval = 10f;

        public int MaxEnergy => _maxEnergy;
        public int EnergyCostPerTap => _energyCostPerTap;
        public int EnergyCostPerAuto => _energyCostPerAuto;
        public int EnergyRestoreAmount => _energyRestoreAmount;
        public float EnergyRestoreInterval => _energyRestoreInterval;

        private void OnValidate()
        {
            _maxEnergy = Mathf.Max(1, _maxEnergy);
            _energyCostPerTap = Mathf.Max(0, _energyCostPerTap);
            _energyCostPerAuto = Mathf.Max(0, _energyCostPerAuto);
            _energyRestoreAmount = Mathf.Max(1, _energyRestoreAmount);
            _energyRestoreInterval = Mathf.Max(0.1f, _energyRestoreInterval);
        }
    }
}
