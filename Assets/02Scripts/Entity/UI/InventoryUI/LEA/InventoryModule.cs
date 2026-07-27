using System;
using System.Collections.Generic;
using UnityEngine;
using Alpha.Slot;

namespace Alpha.Inventory
{
    public class InventoryModule : MonoBehaviour
    {
        #region ======================================== Fields & Properties
        // Slot
        [SerializeField, Min(0)] private int _weaponSlotCount = 10;
        [SerializeField, Min(0)] private int _armorSlotCount = 10;
        [SerializeField, Min(0)] private int _commonSlotCount = 10;

        // Page
        private readonly Dictionary<EItemType, InventoryPage> _inventoryPageDict = new();

        public bool IsInitialized { get; private set; }

        #endregion ======================================== /Fields & Properties

        // 구조 생성
        #region ======================================== Initialize
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
        #endregion ======================================== /Initialize

        // 구조 생성
        #region ======================================== Page
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
        // 생성
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
        // 페이지 선택 추가 생성
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

        // 슬롯 생성
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

        // 아이템 처리
        #region ======================================== Item Add & Remove
        public bool TryAddItem(ItemDTO p_item, int p_requestedCount, out int p_addedCount)
        {
            p_addedCount = 0;
            if (!IsInitialized || p_item == null || p_requestedCount <= 0)
                return false;
            // 입력 검증

            // 1. 아이템에 맞는 SlotGroup 조회
            if (!TryGetTargetSlotGroup(p_item, out SlotGroup slotGroup))
                return false;

            // 2. 기존 동일 아이템 스택부터 채움
            int remainingCount = p_requestedCount;

            int stackedCount = AddToExistingStacks(slotGroup, p_item, remainingCount);

            p_addedCount += stackedCount;
            remainingCount -= stackedCount;
            
            // 3. 남은 수량을 빈 슬롯에 채움
            if (remainingCount > 0)
            {
                p_addedCount += AddToEmptySlots(slotGroup, p_item, remainingCount);
            }

            // 4. 실제 추가 수량 반환

            return p_addedCount > 0;
        }

        // 빈 슬롯에 아이템 입력
        private int AddToEmptySlots(SlotGroup p_slotGroup, ItemDTO p_item, int p_count)
        {
            int addedCount = 0;

            foreach (SlotBase slot in p_slotGroup.SlotList)
            {
                if (!slot.IsEmpty || !slot.CanAdd(p_item))
                    continue;

                addedCount += slot.AddItem(p_item, p_count - addedCount);

                if (addedCount == p_count)
                    break;
            }

            return addedCount;
        }

        // 스택 아이템 슬롯에 아이템 입력
        private int AddToExistingStacks(SlotGroup p_slotGroup, ItemDTO p_item, int p_count)
        {
            int addedCount = 0;

            foreach (SlotBase slot in p_slotGroup.SlotList)
            {
                if (slot.IsEmpty || !slot.CanAdd(p_item))
                    continue;

                addedCount += slot.AddItem(p_item, p_count - addedCount);

                if (addedCount == p_count)
                    break;
            }

            return addedCount;
        }

        public bool TryRemoveItem(SlotBase p_slot, int p_requestedCount, out ItemDTO p_removedItem, out int p_removedCount)
        {
            p_removedItem = null;
            p_removedCount = 0;

            if (!IsInitialized || p_slot == null || p_slot.IsEmpty || p_requestedCount <= 0)
            {
                return false;
            }

            // 다른 인벤토리의 슬롯 변경 방지
            if (!ContainsSlot(p_slot))
                return false;

            // 전부 제거되면 슬롯의 Item이 null이 되므로 먼저 보관
            p_removedItem = p_slot.Item;
            p_removedCount = p_slot.RemoveItem(p_requestedCount);

            if (p_removedCount <= 0)
            {
                p_removedItem = null;
                return false;
            }

            return true;
        }
        #endregion ======================================== /Item Add & Remove

        // 슬롯 간 변경
        #region ======================================== Slot Item Change
        // 빈 슬롯 이동 또는 다른 아이템 교환
        public bool TrySwapSlotItem(SlotBase p_source, SlotBase p_target)
        {
            if (!CanChangeSlotItem(p_source, p_target))
                return false;

            ItemDTO sourceItem = p_source.Item;
            int sourceCount = p_source.Count;

            // 빈 Target으로 전체 이동
            if (p_target.IsEmpty)
            {
                if (!p_target.TryReplace(sourceItem, sourceCount))
                {
                    return false;
                }

                p_source.Clear();
                return true;
            }

            // 동일 아이템은 Merge에서 처리
            if (IsSameItem(sourceItem, p_target.Item))
                return false;

            ItemDTO targetItem = p_target.Item;
            int targetCount = p_target.Count;

            // 서로 상대 아이템을 저장할 수 있는지 확인
            if (!p_source.CanStore(targetItem) || !p_target.CanStore(sourceItem))
            {
                return false;
            }

            if (!p_source.TryReplace(targetItem, targetCount))
            {
                return false;
            }

            if (p_target.TryReplace(sourceItem, sourceCount))
            {
                return true;
            }

            // Target 변경 실패 시 Source 복구
            p_source.TryReplace(sourceItem, sourceCount);

            return false;
        }

        // 동일 아이템 스택 합치기
        public bool TryMergeSlotItem(SlotBase p_source, SlotBase p_target, out int p_mergedCount)
        {
            p_mergedCount = 0;

            if (!CanChangeSlotItem(p_source, p_target) || p_target.IsEmpty)
            {
                return false;
            }

            ItemDTO sourceItem = p_source.Item;

            if (!IsSameItem(sourceItem, p_target.Item) || !p_target.CanAdd(sourceItem))
            {
                return false;
            }

            p_mergedCount = p_target.AddItem(sourceItem, p_source.Count);

            if (p_mergedCount <= 0)
                return false;

            int removedCount = p_source.RemoveItem(p_mergedCount);

            if (removedCount == p_mergedCount)
                return true;

            // 예상하지 못한 실패 시 양쪽 슬롯 복구
            p_target.RemoveItem(p_mergedCount);

            if (removedCount > 0)
                p_source.AddItem(sourceItem, removedCount);

            p_mergedCount = 0;
            return false;

        }
        #endregion ======================================== /Slot Item Change

        // 공통 조회·검증
        #region ======================================== Lookup & Validation
        private bool TryGetTargetSlotGroup(ItemDTO p_item, out SlotGroup p_slotGroup)
        {
            p_slotGroup = null;

            if (!TryGetPage(p_item.ItemType, out InventoryPage page))
                return false;

            int groupIndex;

            switch (p_item.ItemType)
            {
                case EItemType.Weapon when p_item is WeaponDTO weapon:
                    groupIndex = (int)weapon.WeaponType;
                    break;

                case EItemType.Armor when p_item is ArmorDTO armor:
                    groupIndex = (int)armor.ArmorType;
                    break;

                case EItemType.Consumable:
                case EItemType.Material:
                case EItemType.QuestItem:
                    groupIndex = 0;
                    break;

                default:
                    return false;
            }

            return page.TryGetSlotGroup(groupIndex, out p_slotGroup);
        }
        // 슬롯 내부 아이템 조회
        private bool ContainsSlot(SlotBase p_slot)
        {
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
        private static bool IsSameItem(ItemDTO p_left, ItemDTO p_right)
        {
            return p_left != null && p_right != null &&
                   p_left.ItemType == p_right.ItemType &&
                   p_left.Id == p_right.Id;
        }
        private bool CanChangeSlotItem(SlotBase p_source, SlotBase p_target)
        {
            return IsInitialized && p_source != null && p_target != null &&
                   p_source != p_target && !p_source.IsEmpty && ContainsSlot(p_source) &&
                   ContainsSlot(p_target);
        }
        #endregion ======================================== /Lookup & Validation


    }
}
