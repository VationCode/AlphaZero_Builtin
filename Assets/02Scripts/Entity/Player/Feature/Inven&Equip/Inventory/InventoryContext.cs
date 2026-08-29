using System;
using System.Collections.Generic;

namespace Alpha.Player.Inventory
{
    // Player가 보유한 모든 InventorySlot 상태를 보관한다.
    public sealed class InventoryContext
    {
        // ItemType별 전체 슬롯을 관리한다.
        private readonly Dictionary<EItemType, List<InventorySlot>> _slotGroupDict = new();

        // Weapon 슬롯을 Category별로 빠르게 조회하기 위한 보조 인덱스다.
        private readonly Dictionary<EWeaponCategory, List<WeaponInventorySlot>>
            _weaponCategorySlotDict = new();

        private readonly Dictionary<EArmorType, List<ArmorInventorySlot>>
            _armorTypeSlotDict = new();

        private readonly Dictionary<EConsumableType, List<ConsumableInventorySlot>>
            _consumableTypeSlotDict = new();

        private readonly Dictionary<EMaterialType, List<MaterialInventorySlot>>
            _materialTypeSlotDict = new();

        // SlotIndex 기반 단일 슬롯 조회
        private readonly Dictionary<int, InventorySlot> _slotIndexDict = new();

        public event Action<EItemType, InventorySlot> OnSlotAdded;

        // TryGetSlotList 조건을 검사하고 성공 여부와 결과를 반환한다.
        public bool TryGetSlotList(EItemType p_itemType, out IReadOnlyList<InventorySlot> p_slotList)
        {
            if (_slotGroupDict.TryGetValue(p_itemType, out List<InventorySlot> slots))
            {
                p_slotList = slots;
                return true;
            }

            p_slotList = null;
            return false;
        }

        // 지정한 WeaponCategory에 속한 슬롯 목록을 반환한다.
        public bool TryGetWeaponSlotList(
            EWeaponCategory p_weaponCategory,
            out IReadOnlyList<WeaponInventorySlot> p_slotList)
        {
            if (_weaponCategorySlotDict.TryGetValue(
                    p_weaponCategory,
                    out List<WeaponInventorySlot> slots))
            {
                p_slotList = slots;
                return true;
            }

            p_slotList = null;
            return false;
        }

        public bool TryGetArmorSlotList(
            EArmorType p_armorType,
            out IReadOnlyList<ArmorInventorySlot> p_slotList)
        {
            if (_armorTypeSlotDict.TryGetValue(
                    p_armorType,
                    out List<ArmorInventorySlot> slots))
            {
                p_slotList = slots;
                return true;
            }

            p_slotList = null;
            return false;
        }

        public bool TryGetConsumableSlotList(
            EConsumableType p_consumableType,
            out IReadOnlyList<ConsumableInventorySlot> p_slotList)
        {
            if (_consumableTypeSlotDict.TryGetValue(
                    p_consumableType,
                    out List<ConsumableInventorySlot> slots))
            {
                p_slotList = slots;
                return true;
            }

            p_slotList = null;
            return false;
        }

        public bool TryGetMaterialSlotList(
            EMaterialType p_materialType,
            out IReadOnlyList<MaterialInventorySlot> p_slotList)
        {
            if (_materialTypeSlotDict.TryGetValue(
                    p_materialType,
                    out List<MaterialInventorySlot> slots))
            {
                p_slotList = slots;
                return true;
            }

            p_slotList = null;
            return false;
        }

        // TryGetSlot 조건을 검사하고 성공 여부와 결과를 반환한다.
        public bool TryGetSlot(int p_slotIndex, out InventorySlot p_slot)
        {
            if (p_slotIndex < 0)
            {
                p_slot = null;
                return false;
            }

            // _slotGroupDict에서 slotIndex조회시 O(N)속도이기에 평균 O(1)으로 관리

            return _slotIndexDict.TryGetValue(p_slotIndex, out p_slot);
        }

        // AddSlot 대상을 가능한 범위만큼 추가한다.
        internal void AddSlot(EItemType p_itemType, InventorySlot p_slot)
        {
            if (p_slot == null || p_slot.Index < 0)
                return;

            // 동일한 인덱스의 중복 슬롯 등록 방지
            if (_slotIndexDict.ContainsKey(p_slot.Index))
                return;

            // 같은 아이템 타입의 슬롯 그룹이 존재하지 않으면 새로 생성하고 그룹관리에 추가한다.
            if (!_slotGroupDict.TryGetValue(p_itemType, out List<InventorySlot> slotList))
            {
                slotList = new List<InventorySlot>();
                _slotGroupDict.Add(p_itemType, slotList);
            }

            slotList.Add(p_slot);

            // 이벤트 전에 인덱스 조회가 가능해야 한다.
            _slotIndexDict.Add(p_slot.Index, p_slot);

            if (p_itemType == EItemType.Weapon &&
                p_slot is WeaponInventorySlot weaponSlot)
            {
                AddWeaponCategorySlot(weaponSlot);
            }
            else if (p_itemType == EItemType.Armor &&
                     p_slot is ArmorInventorySlot armorSlot)
            {
                AddArmorTypeSlot(armorSlot);
            }
            else if (p_itemType == EItemType.Consumable &&
                     p_slot is ConsumableInventorySlot consumableSlot)
            {
                AddConsumableTypeSlot(consumableSlot);
            }
            else if (p_itemType == EItemType.Material &&
                     p_slot is MaterialInventorySlot materialSlot)
            {
                AddMaterialTypeSlot(materialSlot);
            }

            // 논리 Slot이 추가됐음을 알린다.
            OnSlotAdded?.Invoke(p_itemType, p_slot);
        }

        // Weapon 슬롯의 실제 객체를 Category 보조 인덱스에도 등록한다.
        private void AddWeaponCategorySlot(WeaponInventorySlot p_slot)
        {
            if (!_weaponCategorySlotDict.TryGetValue(
                    p_slot.WeaponCategory,
                    out List<WeaponInventorySlot> slotList))
            {
                slotList = new List<WeaponInventorySlot>();
                _weaponCategorySlotDict.Add(
                    p_slot.WeaponCategory,
                    slotList);
            }

            slotList.Add(p_slot);
        }

        private void AddArmorTypeSlot(ArmorInventorySlot p_slot)
        {
            if (!_armorTypeSlotDict.TryGetValue(
                    p_slot.ArmorType,
                    out List<ArmorInventorySlot> slotList))
            {
                slotList = new List<ArmorInventorySlot>();
                _armorTypeSlotDict.Add(p_slot.ArmorType, slotList);
            }

            slotList.Add(p_slot);
        }

        private void AddConsumableTypeSlot(ConsumableInventorySlot p_slot)
        {
            if (!_consumableTypeSlotDict.TryGetValue(
                    p_slot.ConsumableType,
                    out List<ConsumableInventorySlot> slotList))
            {
                slotList = new List<ConsumableInventorySlot>();
                _consumableTypeSlotDict.Add(
                    p_slot.ConsumableType,
                    slotList);
            }

            slotList.Add(p_slot);
        }

        private void AddMaterialTypeSlot(MaterialInventorySlot p_slot)
        {
            if (!_materialTypeSlotDict.TryGetValue(
                    p_slot.MaterialType,
                    out List<MaterialInventorySlot> slotList))
            {
                slotList = new List<MaterialInventorySlot>();
                _materialTypeSlotDict.Add(p_slot.MaterialType, slotList);
            }

            slotList.Add(p_slot);
        }

        // Clear 상태를 초기값으로 비운다.
        internal void Clear()
        {
            _slotGroupDict.Clear();
            _weaponCategorySlotDict.Clear();
            _armorTypeSlotDict.Clear();
            _consumableTypeSlotDict.Clear();
            _materialTypeSlotDict.Clear();
            _slotIndexDict.Clear();
        }
    }
}
