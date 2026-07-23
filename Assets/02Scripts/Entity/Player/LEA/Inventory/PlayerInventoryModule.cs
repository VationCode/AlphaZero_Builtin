using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Player.Inventory
{
    public class PlayerInventoryModule : MonoBehaviour
    {
        [Header("Slot Count Per Type")]
        [SerializeField, Min(0)] private int _weaponSlotCount = 10;
        [SerializeField, Min(0)] private int _armorSlotCount = 10;
        [SerializeField, Min(0)] private int _commonSlotCount = 10;

        // Weapon과 Armor의 경우는 각 스크롤별로 관리해야 하기에
        private readonly Dictionary<EWeaponType, List<WeaponSlot>> _weaponSlotDict = new();
        private readonly Dictionary<EArmorType, List<ArmorSlot>> _armorSlotDict = new();
        //private readonly Dictionary<EItemType, List<CommonSlot>> _commonSlotDict = new();
        private readonly List<CommonSlot> _consumableSlotList = new();
        private readonly List<CommonSlot> _materialSlotList = new();
        private readonly List<CommonSlot> _questItemSlotList = new();
        public bool IsInitialized { get; private set; }

        public void InitializeSlots()
        {
            if (IsInitialized) return;

            CreateWeaponSlots(EWeaponType.Melee, _weaponSlotCount);
            CreateWeaponSlots(EWeaponType.Range, _weaponSlotCount);
            CreateWeaponSlots(EWeaponType.Special, _weaponSlotCount);

            CreateArmorSlots(EArmorType.Helmet, _armorSlotCount);
            CreateArmorSlots(EArmorType.Chest, _armorSlotCount);
            CreateArmorSlots(EArmorType.Gloves, _armorSlotCount);
            CreateArmorSlots(EArmorType.Boots, _armorSlotCount);

            CreateCommonSlots(EItemType.Consumable, _commonSlotCount);
            CreateCommonSlots(EItemType.Material, _commonSlotCount);
            CreateCommonSlots(EItemType.QuestItem, _commonSlotCount);

            IsInitialized = true;
        }

        public IReadOnlyList<WeaponSlot> CreateWeaponSlots(EWeaponType p_type, int p_count)
        {
            if (p_type == EWeaponType.None || p_count <= 0)
                return Array.Empty<WeaponSlot>();

            // 타입별 슬롯 목록이 없으면 최초 생성
            if (!_weaponSlotDict.TryGetValue(p_type, out var slotList))
            {
                slotList = new List<WeaponSlot>();
                _weaponSlotDict.Add(p_type, slotList);
            }

            List<WeaponSlot> createdSlots = new();

            for (int i = 0; i < p_count; i++)
            {
                WeaponSlot slot = new WeaponSlot(p_type);

                slotList.Add(slot);
                createdSlots.Add(slot);
            }

            return createdSlots;
        }

        public IReadOnlyList<ArmorSlot> CreateArmorSlots(EArmorType p_type, int p_count)
        {
            if (p_type == EArmorType.None || p_count <= 0)
                return Array.Empty<ArmorSlot>();

            // 타입별 슬롯 목록이 없으면 최초 생성
            if (!_armorSlotDict.TryGetValue(p_type, out var slotList))
            {
                slotList = new List<ArmorSlot>();
                _armorSlotDict.Add(p_type, slotList);
            }

            List<ArmorSlot> createdSlots = new();

            for (int i = 0; i < p_count; i++)
            {
                ArmorSlot slot = new ArmorSlot(p_type);

                slotList.Add(slot);
                createdSlots.Add(slot);
            }

            return createdSlots;
        }

        private List<CommonSlot> GetCommonSlotList(EItemType p_type)
        {
            switch (p_type)
            {
                case EItemType.Consumable:
                    return _consumableSlotList;

                case EItemType.Material:
                    return _materialSlotList;

                case EItemType.QuestItem:
                    return _questItemSlotList;

                default:
                    return null;
            }
        }
        public IReadOnlyList<CommonSlot> CreateCommonSlots(EItemType p_type, int p_count)
        {
            List<CommonSlot> slotList = GetCommonSlotList(p_type);

            if (slotList == null || p_count <= 0)
                return Array.Empty<CommonSlot>();

            List<CommonSlot> createdSlots = new();

            for (int i = 0; i < p_count; i++)
            {
                CommonSlot slot = new CommonSlot(p_type);

                slotList.Add(slot);
                createdSlots.Add(slot);
            }

            return createdSlots;
        }

        public IReadOnlyList<WeaponSlot> GetWeaponSlots(EWeaponType p_type)
        {
            return _weaponSlotDict.TryGetValue(p_type, out var slots)? slots : Array.Empty<WeaponSlot>();
        }

        public IReadOnlyList<ArmorSlot> GetArmorSlots(EArmorType p_type)
        {
            return _armorSlotDict.TryGetValue(p_type, out var slots)? slots : Array.Empty<ArmorSlot>();
        }

        public IReadOnlyList<CommonSlot> GetCommonSlots(EItemType p_type)
        {
            return GetCommonSlotList(p_type)?? (IReadOnlyList<CommonSlot>)Array.Empty<CommonSlot>();
        }
    }
}
