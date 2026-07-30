using Alpha.Slot;
using UnityEngine;

namespace Alpha.Equipment
{
    /// <summary>
    /// Inventory Slot과 Equipment Slot 사이의 아이템 이동 및 교환을 실행한다.
    /// Equipment Slot 생성과 조회는 담당하지 않는다.
    /// </summary>
    public class EquipmentItemModule : MonoBehaviour
    {
        private EquipmentSlotModule _slotModule;

        public bool IsBound { get; private set; }

        public bool Bind(EquipmentSlotModule p_slotModule)
        {
            if (p_slotModule == null || !p_slotModule.IsInitialized)
            {
                Debug.LogError($"{nameof(EquipmentItemModule)}에 " + $"{nameof(EquipmentSlotModule)}이 설정되지 않았습니다.", this);
                return false;
            }

            _slotModule = p_slotModule;
            IsBound = true;

            return true;
        }

        /// <summary>
        /// Inventory와 Equipment 사이에서 아이템을 이동하거나 교환한다.
        /// Source 또는 Target 중 정확히 하나가 Equipment Slot이어야 한다.
        /// </summary>
        public bool TrySwapSlotItem(SlotBase p_source, SlotBase p_target)
        {
            if (!CanChangeSlotItem(p_source, p_target))
            {
                return false;
            }

            ItemDTO sourceItem = p_source.Item;
            int sourceCount = p_source.Count;

            // Target이 비어 있으면 아이템 전체를 이동한다.
            if (p_target.IsEmpty)
            {
                if (!p_target.TryReplace(sourceItem, sourceCount))
                {
                    return false;
                }

                p_source.Clear();
                return true;
            }

            ItemDTO targetItem = p_target.Item;
            int targetCount = p_target.Count;

            // 같은 아이템끼리는 장비 상태가 바뀌지 않는다.
            if (p_source.IsSameItem(sourceItem, targetItem))
            {
                return false;
            }

            // 실제 상태를 변경하기 전에 양쪽 저장 가능 여부를 검증한다.
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

            // Target 변경 실패 시 Source를 기존 상태로 복구한다.
            p_source.TryReplace(sourceItem, sourceCount);

            return false;
        }

        private bool CanChangeSlotItem(SlotBase p_source, SlotBase p_target)
        {
            if (!IsBound || p_source == null || p_target == null || ReferenceEquals(p_source, p_target) || p_source.IsEmpty)
                return false;
            

            bool isSourceEquipmentSlot = _slotModule.ContainsSlot(p_source);

            bool isTargetEquipmentSlot = _slotModule.ContainsSlot(p_target);

            // 양쪽 중 정확히 하나만 Equipment Slot이어야 한다.
            // Equipment끼리 또는 Inventory끼리의 교환은 처리하지 않는다.
            return isSourceEquipmentSlot != isTargetEquipmentSlot;
        }
    }
}

/*
Inventory → 빈 Equipment
= 장착

Equipment → 빈 Inventory
= 장착 해제

Inventory ↔ Equipment
= 장비 교환

Inventory ↔ Inventory
= 처리하지 않음

Equipment ↔ Equipment
= 처리하지 않음
 */
