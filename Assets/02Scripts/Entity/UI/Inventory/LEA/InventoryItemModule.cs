using Alpha.Slot;
using UnityEngine;

namespace Alpha.Inventory
{
    /// <summary>
    /// Inventory 아이템의 추가, 제거, 병합, 교환을 실행한다.
    /// Page와 Slot 구조 조회는 InventorySlotModule에 위임한다.
    /// </summary>
    public class InventoryItemModule : MonoBehaviour
    {
        private InventorySlotModule _slotModule;

        public bool IsBound { get; private set; }

        public bool Bind(InventorySlotModule p_slotModule)
        {
            if (p_slotModule == null || !p_slotModule.IsInitialized)
            {
                Debug.LogError($"{nameof(InventoryItemModule)}에 " + $"{nameof(InventorySlotModule)}이 설정되지 않았습니다.", this);
                return false;
            }

            _slotModule = p_slotModule;
            IsBound = true;

            return true;
        }

        #region ============================== Item Add & Remove
        public bool TryAddItem(ItemDTO p_item, int p_requestedCount, out int p_addedCount)
        {
            p_addedCount = 0;

            if (!IsBound || p_item == null || p_requestedCount <= 0)
            {
                return false;
            }

            // 아이템 종류에 맞는 Inventory SlotGroup을 조회한다.
            if (!_slotModule.TryGetTargetSlotGroup(p_item, out SlotGroup slotGroup))
            {
                return false;
            }

            int remainingCount = p_requestedCount;

            // 기존 Stack을 먼저 채워 불필요한 빈 Slot 사용을 막는다.
            int stackedCount = AddToExistingStacks(slotGroup, p_item, remainingCount);

            p_addedCount += stackedCount;
            remainingCount -= stackedCount;

            // 기존 Stack에 모두 넣지 못한 수량은 빈 Slot에 저장한다.
            if (remainingCount > 0)
            {
                p_addedCount += AddToEmptySlots(slotGroup, p_item, remainingCount);
            }

            return p_addedCount > 0;
        }

        public bool TryRemoveItem(SlotBase p_slot, int p_requestedCount, out ItemDTO p_removedItem, out int p_removedCount)
        {
            p_removedItem = null;
            p_removedCount = 0;

            if (!IsBound || p_slot == null || p_slot.IsEmpty || p_requestedCount <= 0)
            {
                return false;
            }

            // 다른 Inventory가 소유한 Slot을 변경하지 않는다.
            if (!_slotModule.ContainsSlot(p_slot))
                return false;

            // 전체 제거 시 Item이 null이 되므로 제거 전에 보관한다.
            p_removedItem = p_slot.Item;
            p_removedCount = p_slot.RemoveItem(p_requestedCount);

            if (p_removedCount > 0) return true;

            p_removedItem = null;
            return false;
        }

        // Statck가능한 아이템 슬롯에 아이템 추가하기
        private static int AddToExistingStacks(SlotGroup p_slotGroup, ItemDTO p_item, int p_count)
        {
            int addedCount = 0;

            foreach (SlotBase slot in p_slotGroup.SlotList)
            {
                // 빈슬롯이거나 추가 가능한지 여부 판단(같은 아이템인지 Stack가능한지)
                if (slot.IsEmpty || !slot.CanAdd(p_item))
                    continue;

                // 같은 아이템 Stack 추가
                addedCount += slot.AddItem(p_item, p_count - addedCount);

                if (addedCount >= p_count)
                    break;
            }

            return addedCount;
        }

        // 빈슬롯에 아이템 추가
        private static int AddToEmptySlots(SlotGroup p_slotGroup, ItemDTO p_item, int p_count)
        {
            int addedCount = 0;

            foreach (SlotBase slot in p_slotGroup.SlotList)
            {
                if (!slot.IsEmpty || !slot.CanAdd(p_item))
                    continue;

                addedCount += slot.AddItem(p_item, p_count - addedCount);

                if (addedCount >= p_count)
                    break;
            }

            return addedCount;
        }
        #endregion ============================== /Item Add & Remove

        // UI에서의 Drag&Drop 이벤트에 의한 아이템 Swap 혹은 Merge
        #region ============================== Slot Item Change

        // 다른 아이템끼리 Swap
        public bool TrySwapSlotItem(SlotBase p_source, SlotBase p_target)
        {
            if (!CanChangeSlotItem(p_source, p_target))
                return false;

            ItemDTO sourceItem = p_source.Item;
            int sourceCount = p_source.Count;

            // 비어 있는 Target으로는 아이템 전체를 이동한다.
            if (p_target.IsEmpty)
            {
                if (!p_target.TryReplace(sourceItem, sourceCount))
                {
                    return false;
                }

                p_source.Clear();
                return true;
            }

            // 같은 아이템의 병합은 TryMergeSlotItem에서 처리한다.
            if (IsSameItem(sourceItem, p_target.Item)) return false;

            ItemDTO targetItem = p_target.Item;
            int targetCount = p_target.Count;

            if (!p_source.CanStore(targetItem) || !p_target.CanStore(sourceItem))
            {
                return false;
            }

            // Source를 먼저 변경하고 Target 변경 실패 시 복구한다.
            if (!p_source.TryReplace(targetItem, targetCount))
            {
                return false;
            }

            if (p_target.TryReplace(sourceItem, sourceCount))
            {
                return true;
            }

            // Target 변경 실패 시 Source를 기존 상태로 복구한다.
            p_source.TryReplace(sourceItem, sourceCount);

            return false;
        }

        // 같은 아이템 병합
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

            // 예상하지 못한 제거 실패 시 두 Slot을 이전 상태로 복구한다.
            p_target.RemoveItem(p_mergedCount);

            if (removedCount > 0)
            {
                p_source.AddItem(sourceItem, removedCount);
            }

            p_mergedCount = 0;
            return false;
        }

        private bool CanChangeSlotItem(SlotBase p_source, SlotBase p_target)
        {
            if (!IsBound || p_source == null || p_target == null || ReferenceEquals(p_source, p_target) || p_source.IsEmpty)
            {
                return false;
            }

            // Inventory 내부 Slot끼리만 변경할 수 있다.
            return _slotModule.ContainsSlot(p_source) && _slotModule.ContainsSlot(p_target);
        }

        // SoureSlot과 TargetSlot가 같은 아이템인지 판단
        private static bool IsSameItem(ItemDTO p_left, ItemDTO p_right)
        {
            return p_left != null && p_right != null && p_left.ItemType == p_right.ItemType && p_left.Id == p_right.Id;
        }
        #endregion ============================== /Slot Item Change
    }
}
