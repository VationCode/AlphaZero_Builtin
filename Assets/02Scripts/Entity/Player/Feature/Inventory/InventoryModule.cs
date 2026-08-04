using UnityEngine;
using System;

namespace Alpha.Player.Inventory
{
    // Player의 Inventory를 관리하는 모듈이다.
    public class InventoryModule : MonoBehaviour
    {
        [Header("Slot Count Per Group")]
        [SerializeField, Min(0)] private int _weaponSlotCount = 10;
        [SerializeField, Min(0)] private int _armorSlotCount = 10;
        [SerializeField, Min(0)] private int _commonSlotCount = 10;

        private InventoryContext _context;
        private int _nextSlotIndex;

        public bool Initialize(InventoryContext p_context)
        {
            if (p_context == null)
                return false;

            _context = p_context;
            _context.Clear();

            _nextSlotIndex = 0;

            CreateWeaponSlots();
            CreateArmorSlots();
            CreateCommonSlots();

            return true;
        }

        // 버튼에 의한 슬롯 추가
        public bool AddSlot(EItemType p_itemType, int p_groupIndex)
        {
            if (_context == null || p_groupIndex < 0)
                return false;

            InventorySlot slot = CreateSlot(p_itemType, p_groupIndex);

            if (slot == null)
                return false;

            _context.AddSlot(p_itemType, slot);

            return true;
        }

        private InventorySlot CreateSlot(EItemType p_itemType, int p_groupIndex)
        {
            switch (p_itemType)
            {
                case EItemType.Weapon:
                    {
                        if (!Enum.IsDefined(typeof(EWeaponType), p_groupIndex))
                        {
                            return null;
                        }

                        return new WeaponInventorySlot(_nextSlotIndex++, (EWeaponType)p_groupIndex);
                    }

                case EItemType.Armor:
                    {
                        if (!Enum.IsDefined(typeof(EArmorType), p_groupIndex))
                        {
                            return null;
                        }

                        return new ArmorInventorySlot(_nextSlotIndex++, (EArmorType)p_groupIndex);
                    }

                case EItemType.Consumable:
                case EItemType.Material:
                case EItemType.QuestItem:
                    {
                        // Common 타입은 ScrollView가 하나이므로 0만 사용한다.
                        if (p_groupIndex != 0)
                            return null;

                        return new CommonInventorySlot(_nextSlotIndex++, p_itemType);
                    }

                default:
                    return null;
            }
        }

        // 초기 셋팅
        private void CreateWeaponSlots()
        {
            AddWeaponSlots(EWeaponType.Melee, _weaponSlotCount);
            AddWeaponSlots(EWeaponType.Range, _weaponSlotCount);
            AddWeaponSlots(EWeaponType.Special, _weaponSlotCount);
        }

        private void AddWeaponSlots(EWeaponType p_weaponType, int p_count)
        {
            for (int i = 0; i < p_count; i++)
            {
                InventorySlot slot = new WeaponInventorySlot(_nextSlotIndex++, p_weaponType);
                _context.AddSlot(EItemType.Weapon, slot);   // 타입 별로 그룹화되어 InventoryContext에 추가된다.
            }
        }

        private void CreateArmorSlots()
        {
            AddArmorSlots(EArmorType.Helmet, _armorSlotCount);
            AddArmorSlots(EArmorType.Chest, _armorSlotCount);
            AddArmorSlots(EArmorType.Gloves, _armorSlotCount);
            AddArmorSlots(EArmorType.Boots, _armorSlotCount);
        }

        private void AddArmorSlots(EArmorType p_armorType, int p_count)
        {
            for (int i = 0; i < p_count; i++)
            {
                InventorySlot slot = new ArmorInventorySlot(_nextSlotIndex++, p_armorType);
                _context.AddSlot(EItemType.Armor, slot);
            }
        }

        private void CreateCommonSlots()
        {
            AddCommonSlots(EItemType.Consumable, _commonSlotCount);
            AddCommonSlots(EItemType.Material, _commonSlotCount);
            AddCommonSlots(EItemType.QuestItem, _commonSlotCount);
        }

        private void AddCommonSlots(EItemType p_itemType, int p_count)
        {
            for (int i = 0; i < p_count; i++)
            {
                InventorySlot slot = new CommonInventorySlot(_nextSlotIndex++, p_itemType);
                _context.AddSlot(p_itemType, slot);
            }
        }


    }
}