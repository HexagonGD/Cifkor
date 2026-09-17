using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

namespace SwiftRiver.Core
{
    public interface ITabManager
    {
        int ActiveTabIndex { get; }
        IReadOnlyReactiveProperty<int> ActiveTabIndexReactive { get; }
        void RegisterTab(ITabView tab);
        void UnregisterTab(ITabView tab);
        void SwitchToTab(int tabIndex);
        ITabView GetActiveTabView();
    }

    public class TabManager : ITabManager, IDisposable
    {
        private readonly List<ITabView> _tabs = new List<ITabView>();
        private readonly ReactiveProperty<int> _activeTabIndex = new ReactiveProperty<int>(-1);

        public int ActiveTabIndex => _activeTabIndex.Value;
        public IReadOnlyReactiveProperty<int> ActiveTabIndexReactive => _activeTabIndex;

        public void RegisterTab(ITabView tab)
        {
            if (tab == null)
                throw new ArgumentNullException(nameof(tab));
            if (_tabs.Contains(tab))
                return;
            _tabs.Add(tab);
        }

        public void UnregisterTab(ITabView tab)
        {
            if (tab == null)
                return;
            int index = _tabs.IndexOf(tab);
            if (index < 0)
                return;
            bool wasActive = index == _activeTabIndex.Value;
            _tabs.RemoveAt(index);
            if (wasActive)
            {
                tab.OnTabDeactivated();
                tab.Hide();
                _activeTabIndex.Value = -1;
            }
            else if (index < _activeTabIndex.Value)
            {
                _activeTabIndex.Value -= 1;
            }
        }

        public void SwitchToTab(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= _tabs.Count)
            {
                Debug.LogWarning($"{nameof(TabManager)} Invalid tab index {tabIndex} (count {_tabs.Count}). Ignored.");
                return;
            }
            if (tabIndex == _activeTabIndex.Value)
                return;

            int previous = _activeTabIndex.Value;
            if (previous >= 0 && previous < _tabs.Count)
            {
                _tabs[previous].OnTabDeactivated();
                _tabs[previous].Hide();
            }

            _activeTabIndex.Value = tabIndex;

            _tabs[tabIndex].Show();
            _tabs[tabIndex].OnTabActivated();
        }

        public ITabView GetActiveTabView()
        {
            int index = _activeTabIndex.Value;
            return index >= 0 && index < _tabs.Count ? _tabs[index] : null;
        }

        public void Dispose()
        {
            _activeTabIndex?.Dispose();
            _tabs.Clear();
        }
    }
}
