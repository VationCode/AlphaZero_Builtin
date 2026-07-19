using System.Collections.Generic;
using UnityEngine;
using System;
using Alpha.UI;

public enum EInventoryView
{
    Closed,
    Category,
    Weapon,
    Armor,
    Consumable,
    Material,
    Quest
}

namespace Alpha.Inventory
{
    public class InventoryView : MonoBehaviour
    {
        [SerializeField] private ViewBase _category;
        [SerializeField] private ViewBase _weaponInventory;
        [SerializeField] private ViewBase _armorInventory;
        [SerializeField] private ViewBase _consumableInventory;
        [SerializeField] private ViewBase _materialInventory;
        [SerializeField] private ViewBase _questInventory;

        private Dictionary<EInventoryView, ViewBase> _viewDict = new ();

        public EInventoryView CurrentView { get; private set; }

        public bool IsOpen => CurrentView != EInventoryView.Closed;

        public WeaponInventoryView WeaponView => _weaponInventory as WeaponInventoryView;
        public ArmorInventoryView ArmorView => _armorInventory as ArmorInventoryView;
        public ItemInventoryView ConsumableView => _consumableInventory as ItemInventoryView;
        public ItemInventoryView MaterialView => _materialInventory as ItemInventoryView;
        public ItemInventoryView QuestView => _questInventory as ItemInventoryView;

        private CursorLockMode _previousCursorLockMode;
        private bool _previousCursorVisible;

        public event Action<bool> OpenStateChanged;
        private void Awake()
        {
            _viewDict.Add(EInventoryView.Category, _category);
            _viewDict.Add(EInventoryView.Weapon, _weaponInventory);
            _viewDict.Add(EInventoryView.Armor, _armorInventory);
            _viewDict.Add(EInventoryView.Consumable, _consumableInventory);
            _viewDict.Add(EInventoryView.Material, _materialInventory);
            _viewDict.Add(EInventoryView.Quest, _questInventory);

            CurrentView = EInventoryView.Closed;

            ApplyView();
        }

        // Unity Button에서 Inventory 화면을 요청한다.
        public void OpenView(int p_view)
        {
            OpenView((EInventoryView)p_view);
        }

        // 지정한 Inventory 화면을 연다.
        public void OpenView(EInventoryView p_view)
        {
            ChangeView(p_view);
        }

        public void CloseView()
        {
            ChangeView(EInventoryView.Closed);
        }

        // 현재 Inventory 화면을 변경한다.
        private void ChangeView(EInventoryView p_view)
        {
            if (CurrentView == p_view)
                return;
            
            bool wasOpen = IsOpen;

            CurrentView = p_view;

            ApplyView();

            if (wasOpen != IsOpen)
            {
                OpenStateChanged?.Invoke(IsOpen);
            }
        }

        // 현재 선택된 화면만 활성화한다.
        private void ApplyView()
        {
            foreach (var view in _viewDict)
            {
                view.Value.gameObject.SetActive(view.Key == CurrentView);
            }

            if (IsOpen)
            {
                _viewDict[CurrentView].Open();
            }
        }
    }
}
