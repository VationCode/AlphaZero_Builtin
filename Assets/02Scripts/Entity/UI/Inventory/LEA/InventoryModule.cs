using Alpha.Slot;
using UnityEngine;

namespace Alpha.Inventory
{
    /// <summary>
    /// Inventory 내부 기능을 하나의 진입점으로 조합한다.
    /// 외부에서는 SlotModule과 ItemModule을 직접 사용하지 않는다.
    /// </summary>
    [RequireComponent(typeof(InventorySlotModule), typeof(InventoryItemModule))]
    public class InventoryModule : MonoBehaviour
    {
        // Slot
        [SerializeField, Min(0)] private int _weaponSlotCount = 10;
        [SerializeField, Min(0)] private int _armorSlotCount = 10;
        [SerializeField, Min(0)] private int _commonSlotCount = 10;

        private InventorySlotModule _slotModule;
        private InventoryItemModule _itemModule;

        public bool IsInitialized { get; private set; }

        private void Awake()
        {
            _slotModule = GetComponent<InventorySlotModule>();
            _itemModule = GetComponent<InventoryItemModule>();
        }

        /// <summary>
        /// Slot 구조를 먼저 만든 뒤 아이템 기능을 연결한다.
        /// 중간 단계가 실패하면 초기화 완료로 처리하지 않는다.
        /// </summary>
        public bool Initialize()
        {
            if (IsInitialized) return true;

            if (_slotModule == null || _itemModule == null)
            {
                Debug.LogError($"{nameof(InventoryModule)}의 " + "내부 Module이 설정되지 않았습니다.", this);
                return false;
            }

            if (!_slotModule.Initialize(_weaponSlotCount, _armorSlotCount, _commonSlotCount))
                return false;


            if (!_itemModule.Bind(_slotModule)) return false;

            IsInitialized = true;
            return true;
        }

        #region ============================== Page & Slot
        /// <summary>
        /// Presenter가 화면에 연결할 Inventory Page를 조회한다.
        /// </summary>
        public bool TryGetPage(EItemType p_pageType, out InventoryPage p_page)
        {
            p_page = null;

            return IsInitialized && _slotModule.TryGetPage(p_pageType, out p_page);
        }

        /// <summary>
        /// 지정된 Page와 Group에 새로운 Slot을 추가한다.
        /// </summary>
        public SlotBase AddSlot(EItemType p_pageType, int p_groupIndex)
        {
            if (!IsInitialized) return null;

            return _slotModule.AddSlot(p_pageType, p_groupIndex);
        }
        #endregion ============================== /Page & Slot

        #region ============================== Item Add & Remove
        public bool TryAddItem(ItemDTO p_item, int p_requestedCount, out int p_addedCount)
        {
            p_addedCount = 0;

            return IsInitialized && _itemModule.TryAddItem(p_item, p_requestedCount, out p_addedCount);
        }

        public bool TryRemoveItem(SlotBase p_slot, int p_requestedCount, out ItemDTO p_removedItem, out int p_removedCount)
        {
            p_removedItem = null;
            p_removedCount = 0;

            return IsInitialized &&
                   _itemModule.TryRemoveItem(p_slot, p_requestedCount, out p_removedItem, out p_removedCount);
        }
        #endregion ============================== /Item Add & Remove

        #region ============================== Slot Item Change
        /// <summary>
        /// 다른 아이템끼리 Swap
        /// </summary>
        public bool TrySwapSlotItem(SlotBase p_source, SlotBase p_target)
        {
            return IsInitialized && _itemModule.TrySwapSlotItem(p_source, p_target);
        }

        /// <summary>
        /// 같은 아이템 병합
        /// </summary>
        public bool TryMergeSlotItem(SlotBase p_source, SlotBase p_target, out int p_mergedCount)
        {
            p_mergedCount = 0;

            return IsInitialized &&
                   _itemModule.TryMergeSlotItem(p_source, p_target, out p_mergedCount);
        }
        #endregion ============================== /Slot Item Change

    }
}
