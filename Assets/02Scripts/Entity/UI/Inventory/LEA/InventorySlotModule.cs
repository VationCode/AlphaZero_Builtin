using System.Collections.Generic;
using Alpha.Slot;
using UnityEngine;

namespace Alpha.Inventory
{
    /// <summary>
    /// Inventory의 Page, SlotGroup, Slot 구조를 생성하고 조회한다.
    /// 아이템 추가·제거와 Slot 간 이동은 담당하지 않는다.
    /// </summary>
    public class InventorySlotModule : MonoBehaviour
    {
        private readonly Dictionary<EItemType, InventoryPage> _inventoryPageDict = new();

        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Inventory가 사용할 전체 Slot 구조를 생성한다.
        /// Slot 개수는 기존 Inspector 값을 보존하기 위해 InventoryModule로부터 전달받는다.
        /// </summary>
        public bool Initialize(int p_weaponSlotCount, int p_armorSlotCount, int p_commonSlotCount)
        {
            if (IsInitialized)
                return true;

            if (p_weaponSlotCount < 0 || p_armorSlotCount < 0 || p_commonSlotCount < 0)
            {
                Debug.LogError($"{nameof(InventorySlotModule)}의 Slot 개수가 올바르지 않습니다.", this);
                return false;
            }

            CreateWeaponPage(p_weaponSlotCount);
            CreateArmorPage(p_armorSlotCount);

            CreateSingleGroupPage(EItemType.Consumable, p_commonSlotCount);

            CreateSingleGroupPage(EItemType.Material, p_commonSlotCount);

            CreateSingleGroupPage(EItemType.QuestItem, p_commonSlotCount);

            IsInitialized = true;
            return true;
        }

        #region ======================================== Page
        public bool TryGetPage(EItemType p_pageType, out InventoryPage p_page)
        {
            p_page = null;

            return IsInitialized && _inventoryPageDict.TryGetValue(p_pageType, out p_page);
        }

        private InventoryPage CreatePage(EItemType p_pageType)
        {
            if (p_pageType == EItemType.None || _inventoryPageDict.ContainsKey(p_pageType))
            {
                return null;
            }

            InventoryPage page = new();

            _inventoryPageDict.Add(p_pageType, page);

            return page;
        }

        private void CreateWeaponPage(int p_slotCount)
        {
            InventoryPage page = CreatePage(EItemType.Weapon);

            if (page == null) return;

            page.AddSlotGroup((int)EWeaponType.Melee, CreateWeaponSlotGroup(EWeaponType.Melee, p_slotCount));
            page.AddSlotGroup((int)EWeaponType.Range, CreateWeaponSlotGroup(EWeaponType.Range, p_slotCount));
            page.AddSlotGroup((int)EWeaponType.Special, CreateWeaponSlotGroup(EWeaponType.Special, p_slotCount));
        }

        private void CreateArmorPage(int p_slotCount)
        {
            InventoryPage page = CreatePage(EItemType.Armor);

            if (page == null)
                return;

            page.AddSlotGroup((int)EArmorType.Helmet, CreateArmorSlotGroup(EArmorType.Helmet, p_slotCount));
            page.AddSlotGroup((int)EArmorType.Chest, CreateArmorSlotGroup(EArmorType.Chest, p_slotCount));
            page.AddSlotGroup((int)EArmorType.Gloves, CreateArmorSlotGroup(EArmorType.Gloves, p_slotCount));
            page.AddSlotGroup((int)EArmorType.Boots, CreateArmorSlotGroup(EArmorType.Boots, p_slotCount));
        }

        // ConsumablePage, MaterialPage, QuestItemPage
        private void CreateSingleGroupPage(EItemType p_pageType, int p_slotCount)
        {
            InventoryPage page = CreatePage(p_pageType);

            if (page == null) return;

            page.AddSlotGroup(0, CreateCommonSlotGroup(p_pageType, p_slotCount));
        }
        #endregion ======================================== /Page

        #region ======================================== SlotGroup

        private SlotGroup CreateWeaponSlotGroup(EWeaponType p_weaponType, int p_slotCount)
        {
            SlotGroup slotGroup = new();

            for (int i = 0; i < p_slotCount; i++)
            {
                slotGroup.AddSlot(SlotFactory.CreateWeaponSlot(p_weaponType));
            }

            return slotGroup;
        }

        private SlotGroup CreateArmorSlotGroup(EArmorType p_armorType, int p_slotCount)
        {
            SlotGroup slotGroup = new();

            for (int i = 0; i < p_slotCount; i++)
            {
                slotGroup.AddSlot(SlotFactory.CreateArmorSlot(p_armorType));
            }

            return slotGroup;
        }

        private SlotGroup CreateCommonSlotGroup(EItemType p_itemType, int p_slotCount)
        {
            SlotGroup slotGroup = new();

            for (int i = 0; i < p_slotCount; i++)
            {
                slotGroup.AddSlot(SlotFactory.CreateCommonSlot(p_itemType));
            }

            return slotGroup;
        }

        /// <summary>
        /// 아이템 종류에 맞는 SlotGroup을 조회한다.
        /// InventoryItemModule이 아이템을 저장할 위치를 찾을 때 사용한다.
        /// </summary>
        internal bool TryGetTargetSlotGroup(ItemDTO p_item, out SlotGroup p_slotGroup)
        {
            p_slotGroup = null;

            if (!IsInitialized || p_item == null)
                return false;

            if (!TryGetPage(p_item.ItemType, out InventoryPage page))
            {
                return false;
            }

            int groupIndex;

            switch (p_item)
            {
                case WeaponDTO weapon:
                    groupIndex = (int)weapon.WeaponType;
                    break;

                case ArmorDTO armor:
                    groupIndex = (int)armor.ArmorType;
                    break;

                default:
                    groupIndex = 0;
                    break;
            }

            return page.TryGetSlotGroup(groupIndex, out p_slotGroup);
        }

        #endregion ======================================== /SlotGroup

        #region ======================================== Slot

        public SlotBase AddSlot(EItemType p_pageType, int p_groupIndex)
        {
            if (!IsInitialized || !TryGetPage(p_pageType, out InventoryPage page))
            {
                return null;
            }

            if (!page.TryGetSlotGroup(p_groupIndex, out SlotGroup slotGroup))
            {
                return null;
            }

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
                EItemType.Weapon =>
                    SlotFactory.CreateWeaponSlot((EWeaponType)p_groupIndex),

                EItemType.Armor =>
                    SlotFactory.CreateArmorSlot((EArmorType)p_groupIndex),

                EItemType.Consumable or EItemType.Material or
                EItemType.QuestItem => SlotFactory.CreateCommonSlot(p_pageType),

                _ => null
            };
        }

        /// <summary>
        /// 전달된 Slot이 현재 Inventory에 포함된 Slot인지 검사한다.
        /// 다른 Inventory의 Slot을 변경하는 것을 방지한다.
        /// </summary>
        internal bool ContainsSlot(SlotBase p_slot)
        {
            if (!IsInitialized || p_slot == null)
                return false;

            foreach (InventoryPage page in _inventoryPageDict.Values)
            {
                foreach (SlotGroup slotGroup in page.SlotGroupDict.Values)
                {
                    if (slotGroup.SlotList.Contains(p_slot))
                        return true;
                }
            }

            return false;
        }

        #endregion ======================================== /Slot
    }
}
