using Alpha.Mouse;
using Alpha.Player.Slot;
using System;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

namespace Alpha.Player.Inventory
{
    // 입력에 따른 창 활성화 판단
    // InventoryModule에 슬롯 초기화·추가 요청
    // 생성된 슬롯을 외부에 이벤트로 전달

    public class PlayerInventoryFlow : MonoBehaviour
    {
        private AlphaInputSystem _input;
        private PlayerInventoryModule _inventoryModule;

        public bool IsOpen { get; private set; }

        // InventoryWindow 활성화 관리
        public event Action<bool> OnWindowActivate;

        // Slot 관리
        public event Action OnSlotsInitialized;
        public event Action<IReadOnlyList<SlotBase>> OnSlotsAdded;

        public void Bind(PlayerCore p_core)
        {
            if (p_core == null) return;

            _input = p_core.Input;
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

        #region ======================================== Slot
        public void InitializeSlots()
        {
            if (_inventoryModule == null || _inventoryModule.IsInitialized)
                return;

            // Flow가 Module의 초기화를 실행
            //_inventoryModule.InitializeSlots();

            // 초기화가 끝난 후 외부에 알림
            OnSlotsInitialized?.Invoke();
        }

        public void AddWeaponSlots(EWeaponType p_type, int p_count = 1)
        {
            if (_inventoryModule == null)
                return;

            //IReadOnlyList<WeaponSlot> slots = _inventoryModule.CreateWeaponSlotList(p_type, p_count);

            //NotifySlotsAdded(slots);
        }

        public void AddArmorSlots(EArmorType p_type, int p_count = 1)
        {
            if (_inventoryModule == null || p_count <= 0)
                return;

            //IReadOnlyList<ArmorSlot> slots = _inventoryModule.CreateArmorSlotList(p_type, p_count);

            //NotifySlotsAdded(slots);
        }

        public void AddCommonSlots(EItemType p_type, int p_count = 1)
        {
            if (_inventoryModule == null || p_count <= 0)
                return;

            //IReadOnlyList<CommonSlot> slots = _inventoryModule.CreateCommonSlotList(p_type, p_count);

            //NotifySlotsAdded(slots);
        }

        // Add 내용에 대해 외부에 알림
        private void NotifySlotsAdded<TSlot>(IReadOnlyList<TSlot> p_slots) where TSlot : SlotBase
        {
            if (p_slots == null || p_slots.Count == 0)
            {
                return;
            }

            OnSlotsAdded?.Invoke(p_slots);
        }
        #endregion ======================================== /Slot

    }
}