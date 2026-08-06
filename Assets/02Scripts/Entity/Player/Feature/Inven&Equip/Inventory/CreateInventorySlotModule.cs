using System;
using UnityEngine;

namespace Alpha.Player.Inventory
{
    // 슬롯 생성과 확장을 담당
    public class CreateInventorySlotModule
    {
        private int _weaponSlotCount;
        private int _armorSlotCount;
        private int _commonSlotCount;

        private InventoryContext _context;
        private int _nextSlotIndex;

        // 전달받은 값으로 초기 상태를 구성한다.
        public CreateInventorySlotModule(int p_weaponSlotCount, int p_armorSlotCount, int p_commonSlotCount)
        {
            _weaponSlotCount = p_weaponSlotCount;
            _armorSlotCount = p_armorSlotCount;
            _commonSlotCount = p_commonSlotCount;
        }

        // 기존 슬롯을 비우고 아이템 분류별 기본 슬롯을 다시 생성한다.
        public bool Initialize(InventoryContext p_context)
        {
            if (p_context == null)
                return false;

            _context = p_context;

            // 재초기화 시 기존 슬롯과 Index를 먼저 초기화한다.
            _context.Clear();

            _nextSlotIndex = 0;

            // 아이템 분류별 설정 수량만큼 기본 슬롯을 생성한다.
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

        // 아이템 종류와 세부 그룹에 맞는 슬롯 구현체를 생성한다.
        private InventorySlot CreateSlot(EItemType p_itemType, int p_groupIndex)
        {
            // 무기·방어구는 세부 타입별 슬롯, 나머지는 공용 슬롯을 사용한다.
            switch (p_itemType)
            {
                case EItemType.Weapon:
                    if (!Enum.IsDefined(typeof(EWeaponType), p_groupIndex))
                    {
                        return null;
                    }

                    return new WeaponInventorySlot(_nextSlotIndex++, (EWeaponType)p_groupIndex);

                case EItemType.Armor:
                    if (!Enum.IsDefined(typeof(EArmorType), p_groupIndex))
                    {
                        return null;
                    }

                    return new ArmorInventorySlot(_nextSlotIndex++, (EArmorType)p_groupIndex);

                case EItemType.Consumable:
                case EItemType.Material:
                case EItemType.QuestItem:

                    // Common 타입은 ScrollView가 하나이므로 0만 사용한다.
                    if (p_groupIndex != 0)
                        return null;

                    return new CommonInventorySlot(_nextSlotIndex++, p_itemType);

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

        // AddWeaponSlots 대상을 가능한 범위만큼 추가한다.
        private void AddWeaponSlots(EWeaponType p_weaponType, int p_count)
        {
            for (int i = 0; i < p_count; i++)
            {
                InventorySlot slot = new WeaponInventorySlot(_nextSlotIndex++, p_weaponType);
                _context.AddSlot(EItemType.Weapon, slot);   // 타입 별로 그룹화되어 InventoryContext에 추가된다.
            }
        }

        // CreateArmorSlots 객체 또는 데이터를 생성한다.
        private void CreateArmorSlots()
        {
            AddArmorSlots(EArmorType.Helmet, _armorSlotCount);
            AddArmorSlots(EArmorType.Chest, _armorSlotCount);
            AddArmorSlots(EArmorType.Gloves, _armorSlotCount);
            AddArmorSlots(EArmorType.Boots, _armorSlotCount);
        }

        // AddArmorSlots 대상을 가능한 범위만큼 추가한다.
        private void AddArmorSlots(EArmorType p_armorType, int p_count)
        {
            for (int i = 0; i < p_count; i++)
            {
                InventorySlot slot = new ArmorInventorySlot(_nextSlotIndex++, p_armorType);
                _context.AddSlot(EItemType.Armor, slot);
            }
        }

        // CreateCommonSlots 객체 또는 데이터를 생성한다.
        private void CreateCommonSlots()
        {
            AddCommonSlots(EItemType.Consumable, _commonSlotCount);
            AddCommonSlots(EItemType.Material, _commonSlotCount);
            AddCommonSlots(EItemType.QuestItem, _commonSlotCount);
        }

        // AddCommonSlots 대상을 가능한 범위만큼 추가한다.
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
