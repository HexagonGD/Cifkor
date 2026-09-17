using System;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace SwiftRiver.Core.NavigationBar
{
    public class NavigationBarView : MonoBehaviour, INavigationBarView
    {
        [SerializeField] private Button[] _tabButtons;
        [SerializeField] private TextMeshProUGUI[] _tabLabels;
        [SerializeField] private Color _activeColor = Color.white;
        [SerializeField] private Color _inactiveColor = Color.gray;

        private readonly Subject<int> _tabSelected = new Subject<int>();
        private int _currentTab = -1;

        public IObservable<int> OnTabSelected => _tabSelected;

        private void Awake()
        {
            if (_tabButtons == null)
            {
                Debug.LogError($"{nameof(NavigationBarView)} Tab buttons not assigned.", this);
                return;
            }
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                if (_tabButtons[i] == null)
                {
                    Debug.LogWarning($"{nameof(NavigationBarView)} Button {i} is null.", this);
                    continue;
                }
                int tabIndex = i;
                _tabButtons[i].onClick.AddListener(() => SelectTab(tabIndex));
            }
        }

        private void SelectTab(int tabIndex)
        {
            if (tabIndex == _currentTab)
                return;
            _tabSelected.OnNext(tabIndex);
        }

        public void SetActiveTab(int tabIndex)
        {
            _currentTab = tabIndex;
            if (_tabButtons == null)
                return;
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                if (_tabButtons[i] == null)
                    continue;
                bool isActive = i == tabIndex;

                _tabButtons[i].interactable = !isActive;

                if (_tabButtons[i].image != null)
                    _tabButtons[i].image.color = isActive ? _activeColor : _inactiveColor;

                if (_tabLabels != null && i < _tabLabels.Length && _tabLabels[i] != null)
                    _tabLabels[i].color = isActive ? _activeColor : _inactiveColor;
            }
        }

        private void OnDestroy()
        {
            if (_tabButtons != null)
            {
                foreach (var button in _tabButtons)
                {
                    if (button != null)
                        button.onClick.RemoveAllListeners();
                }
            }
            _tabSelected?.OnCompleted();
            _tabSelected?.Dispose();
        }
    }
}
