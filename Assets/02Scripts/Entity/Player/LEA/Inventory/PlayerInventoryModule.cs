using Alpha.Player.Slot;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Player.Inventory
{
    public class PlayerInventoryModule : MonoBehaviour
    {
        // Slot
        [SerializeField, Min(0)] private int _weaponSlotCount = 10;
        [SerializeField, Min(0)] private int _armorSlotCount = 10;
        [SerializeField, Min(0)] private int _commonSlotCount = 10;

        // Page
        private readonly Dictionary<EItemType, InventoryPage> _inventoryPageDict = new();

        public bool IsInitialized { get; private set; }

       
        public void Initialize()
        {
            if (IsInitialized)
                return;

            CreateWeaponPage();
            CreateArmorPage();

            CreateSingleGroupPage(EItemType.Consumable);
            CreateSingleGroupPage(EItemType.Material);
            CreateSingleGroupPage(EItemType.QuestItem);

            IsInitialized = true;
        }

        #region ======================================== InventoryPage
        // Page 조회
        public bool TryGetPage(EItemType p_pageType, out InventoryPage p_page)
        {
            return _inventoryPageDict.TryGetValue(p_pageType, out p_page);
        }

        // 무기 Page 및 무기 종류별 Group 생성
        private void CreateWeaponPage()
        {
            InventoryPage page = CreatePage(EItemType.Weapon);

            if (page == null)
                return;

            page.AddSlotGroup((int)EWeaponType.Melee, CreateWeaponSlotGroup(EWeaponType.Melee));
            page.AddSlotGroup((int)EWeaponType.Range, CreateWeaponSlotGroup(EWeaponType.Range));
            page.AddSlotGroup((int)EWeaponType.Special, CreateWeaponSlotGroup(EWeaponType.Special));
        }

        // 방어구 Page 및 방어구 종류별 Group 생성
        private void CreateArmorPage()
        {
            InventoryPage page = CreatePage(EItemType.Armor);

            if (page == null)
                return;

            page.AddSlotGroup((int)EArmorType.Helmet, CreateArmorSlotGroup(EArmorType.Helmet));
            page.AddSlotGroup((int)EArmorType.Chest, CreateArmorSlotGroup(EArmorType.Chest));
            page.AddSlotGroup((int)EArmorType.Gloves, CreateArmorSlotGroup(EArmorType.Gloves));
            page.AddSlotGroup((int)EArmorType.Boots, CreateArmorSlotGroup(EArmorType.Boots));
        }

        // 하나의 Group만 사용하는 Page 생성
        private void CreateSingleGroupPage(EItemType p_pageType)
        {
            InventoryPage page = CreatePage(p_pageType);

            if (page == null)
                return;

            page.AddSlotGroup(0, CreateCommonSlotGroup(p_pageType));
        }

        // Page 생성 및 등록
        private InventoryPage CreatePage(EItemType p_pageType)
        {
            if (p_pageType == EItemType.None || _inventoryPageDict.ContainsKey(p_pageType))
                return null;

            InventoryPage page = new InventoryPage();

            _inventoryPageDict.Add(p_pageType, page);

            return page;
        }

        #endregion ======================================== /InventoryPage

        #region ======================================== SlotGroup
        private SlotGroup CreateWeaponSlotGroup(EWeaponType p_type)
        {
            SlotGroup slotGroup = new();

            for (int i = 0; i < _weaponSlotCount; i++)
            {
                WeaponSlot slot = SlotFactory.CreateWeaponSlot(p_type);

                slotGroup.AddSlot(slot);
            }

            return slotGroup;
        }

        private SlotGroup CreateArmorSlotGroup(EArmorType p_type)
        {
            SlotGroup slotGroup = new();

            for (int i = 0; i < _armorSlotCount; i++)
            {
                ArmorSlot slot = SlotFactory.CreateArmorSlot(p_type);

                slotGroup.AddSlot(slot);
            }

            return slotGroup;
        }

        private SlotGroup CreateCommonSlotGroup(EItemType p_type)
        {
            SlotGroup slotGroup = new();

            for (int i = 0; i < _commonSlotCount; i++)
            {
                CommonSlot slot = SlotFactory.CreateCommonSlot(p_type);

                slotGroup.AddSlot(slot);
            }

            return slotGroup;
        }
        #endregion ======================================== /SlotGroup

        #region ======================================== Slot
        public SlotBase AddSlot(EItemType p_pageType, int p_groupIndex)
        {
            if (!TryGetPage(p_pageType, out InventoryPage page))
                return null;

            if (!page.TryGetSlotGroup(p_groupIndex, out SlotGroup slotGroup))
                return null;

            SlotBase slot = CreateSlot(p_pageType, p_groupIndex);

            if (slot == null)
                return null;

            slotGroup.AddSlot(slot);

            return slot;
        }

        private SlotBase CreateSlot(EItemType p_pageType, int p_groupIndex)
        {
            return p_pageType switch
            {
                EItemType.Weapon => SlotFactory.CreateWeaponSlot((EWeaponType)p_groupIndex),

                EItemType.Armor =>SlotFactory.CreateArmorSlot((EArmorType)p_groupIndex),

                EItemType.Consumable or EItemType.Material or EItemType.QuestItem => SlotFactory.CreateCommonSlot(p_pageType),

                _ => null
            };
        }
        #endregion ======================================== /Slot
    }
}
