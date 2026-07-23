using Alpha.Mouse;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Player.Inventory
{
    public class PlayerInventoryFlow : MonoBehaviour
    {
        private PlayerCore _core;
        private AlphaInputSystem _input;
        private MouseSystem _mouseSystem;
        private PlayerInventoryModule _inventoryModule;

        public bool IsOpen { get; private set; }

        // InventoryWindow 활성화 관리
        public event Action<bool> OnWindowActivate;

        // Slot 관리
        public event Action OnSlotsInitialized;
        public event Action<IReadOnlyList<SlotBase>> OnSlotsAdded;

        public void Bind(PlayerCore p_core)
        {
            _core = p_core;
            _input = p_core.Input;
            _mouseSystem = p_core.MouseSystem;
            _inventoryModule = p_core.InventoryModule;
        }

        private void Update()
        {
            if (_input != null && _input.IsInventory)
                SetWindowActive(!IsOpen);
        }

        // 인벤토리 창
        public void SetWindowActive(bool p_isActive)
        {
            if (IsOpen == p_isActive)
                return;

            IsOpen = p_isActive;
            OnWindowActivate?.Invoke(IsOpen);
        }

        // Slot
        public void InitializeSlots()
        {
            if (_inventoryModule == null || _inventoryModule.IsInitialized)
                return;

            // Flow가 Module의 초기화를 실행
            _inventoryModule.InitializeSlots();

            // 초기화가 끝난 후 외부에 알림
            OnSlotsInitialized?.Invoke();
        }

        public void AddWeaponSlots(EWeaponType p_type, int p_count = 1)
        {
            if (_inventoryModule == null || p_count <= 0)
                return;

            var createdSlots = _inventoryModule.CreateWeaponSlots(p_type, p_count);

            // 생성된 슬롯 목록을 한 번만 전달
            if (createdSlots.Count > 0)
                OnSlotsAdded?.Invoke(createdSlots);
        }

        public void AddArmorSlots(EArmorType p_type, int p_count = 1)
        {
            if (_inventoryModule == null || p_count <= 0)
                return;

            var createdSlots = _inventoryModule.CreateArmorSlots(p_type, p_count);

            if (createdSlots.Count > 0)
                OnSlotsAdded?.Invoke(createdSlots);
        }

        public void AddCommonSlots(EItemType p_type, int p_count = 1)
        {
            if (_inventoryModule == null || p_count <= 0)
                return;

            var createdSlots = _inventoryModule.CreateCommonSlots(p_type, p_count);

            if (createdSlots.Count > 0)
                OnSlotsAdded?.Invoke(createdSlots);
        }

       
    }
}