using System;
using Alpha.Item.Armor;
using Alpha.Player.Inventory;
using UnityEngine;

namespace Alpha.Player.Equipment
{
    // 실제 이동·보관·교환 및 실패 복구 담당
    public class EquipmentModule : MonoBehaviour
    {
        private EquipmentContext _context;
        private InventoryModule _inventoryModule;
        private SlotTransferModule _transferModule;
        private ResourceLoadSystem _resourceLoader;

        public int TotalArmorDefense { get; private set; }
        public event Action<int> OnArmorDefenseChanged;

        // 외부 의존성을 연결하고 사용할 수 있는 상태로 준비한다.
        public bool Bind(
            EquipmentContext p_context,
            InventoryModule p_inventoryModule,
            SlotTransferModule p_transferModule,
            ResourceLoadSystem p_resourceLoader)
        {
            if (p_context == null ||
                p_inventoryModule == null ||
                p_transferModule == null ||
                p_resourceLoader == null)
            {
                return false;
            }

            Unbind();

            _context = p_context;
            _inventoryModule = p_inventoryModule;
            _transferModule = p_transferModule;
            _resourceLoader = p_resourceLoader;

            _context.OnSlotChanged += HandleSlotChanged;
            RecalculateArmorDefense();

            return true;
        }

        public void Unbind()
        {
            if (_context != null)
                _context.OnSlotChanged -= HandleSlotChanged;

            _context = null;
            _inventoryModule = null;
            _transferModule = null;
            _resourceLoader = null;
            TotalArmorDefense = 0;
        }

        // DTO의 세부 장비 타입에 대응하는 Context 슬롯을 Flow에 제공한다.
        internal bool TryGetTargetSlot(ItemDTO p_item, out EquipmentSlot p_slot)
        {
            // DTO 형식과 세부 장비 종류를 사용해 대응하는 Context 슬롯을 조회한다.
            switch (p_item)
            {
                case WeaponDTO weapon:
                    if (_context.TryGetWeaponSlot(weapon.WeaponType, out WeaponEquipmentSlot weaponSlot))
                    {
                        p_slot = weaponSlot;
                        return true;
                    }

                    break;

                case ArmorDTO armor:
                    if (_context.TryGetArmorSlot(armor.ArmorType, out ArmorEquipmentSlot armorSlot))
                    {
                        p_slot = armorSlot;
                        return true;
                    }

                    break;
            }

            p_slot = null;
            return false;
        }

        // 더블 클릭으로 장비를 해제할 때 사용할 인벤토리 슬롯을 찾는다.
        internal bool TryGetInventoryTargetSlot(ItemDTO p_item, out InventorySlot p_slot)
        {
            if (_inventoryModule == null)
            {
                p_slot = null;
                return false;
            }

            return _inventoryModule.TryGetStorageSlot(p_item, out p_slot);
        }

        // Inventory와 Equipment 슬롯을 구분하지 않고 공통 이동 규칙을 실행한다.
        internal bool Transfer(ItemSlot p_source, ItemSlot p_target)
        {
            return _transferModule != null &&
                   _transferModule.Transfer(p_source, p_target);
        }

        private void HandleSlotChanged(EquipmentSlot p_slot)
        {
            if (p_slot is ArmorEquipmentSlot)
                RecalculateArmorDefense();
        }

        // 장착된 Armor Prefab의 Inspector 수치를 합산한다.
        private void RecalculateArmorDefense()
        {
            int totalDefense = 0;

            if (_context != null && _resourceLoader != null)
            {
                foreach (EquipmentSlot slot in _context.Slots)
                {
                    if (slot is not ArmorEquipmentSlot armorSlot ||
                        armorSlot.Armor == null)
                    {
                        continue;
                    }

                    GameObject prefab = _resourceLoader.GetItemPrefab(
                        EItemType.Armor,
                        armorSlot.Armor.PrefabKey);

                    ArmorItem armorItem =
                        prefab?.GetComponent<ArmorItem>();

                    if (armorItem == null)
                        continue;

                    totalDefense += armorItem.BaseDefense;
                }
            }

            if (TotalArmorDefense == totalDefense)
                return;

            TotalArmorDefense = totalDefense;
            OnArmorDefenseChanged?.Invoke(TotalArmorDefense);
        }
    }
}
