using UnityEngine;

namespace Alpha.Player.Inventory
{
    public class InventoryModule : MonoBehaviour
    {
        [Header("Create Slot")]
        [SerializeField, Min(0)]
        private int _weaponSlotCount = 10;

        [SerializeField, Min(0)]
        private int _armorSlotCount = 10;

        [SerializeField, Min(0)]
        private int _commonSlotCount = 10;

        private CreateInventorySlotModule _slotModule;
        private InventoryStorageModule _storageModule;
        private SlotTransferModule _transferModule;

        // Unity 초기화 시 필요한 컴포넌트와 내부 객체를 준비한다.
        private void Awake()
        {
            // 슬롯 생성 조회 담당
            _slotModule = new CreateInventorySlotModule(_weaponSlotCount, _armorSlotCount, _commonSlotCount);
        }

        // 슬롯을 생성하고 저장·이동 Module을 현재 Context에 연결한다.
        public bool Initialize(InventoryContext p_context, SlotTransferModule p_transferModule)
        {
            if (p_context == null || p_transferModule == null)
                return false;

            if (!_slotModule.Initialize(p_context))
                return false;

            _storageModule = new InventoryStorageModule(p_context);
            _transferModule = p_transferModule;

            return true;
        }

        // 지정 아이템 그룹에 새 인벤토리 슬롯을 추가한다.
        public bool AddSlot(EItemType p_itemType, int p_groupIndex)
        {
            return _slotModule != null && _slotModule.AddSlot(p_itemType, p_groupIndex);
        }

        // 아이템을 같은 스택과 빈 슬롯 순서로 가능한 만큼 보관한다.
        public int AddItem(ItemDTO p_item, int p_count)
        {
            if (_storageModule == null)
                return 0;

            return _storageModule.AddItem(p_item, p_count);
        }

        // 자동 이동에 사용할 인벤토리 보관 슬롯을 찾는다.
        internal bool TryGetStorageSlot(ItemDTO p_item, out InventorySlot p_slot)
        {
            if (_storageModule == null)
            {
                p_slot = null;
                return false;
            }

            return _storageModule.TryGetTargetSlot(p_item, out p_slot);
        }

        // 두 공통 슬롯의 상태에 따라 이동·병합·교환을 실행한다.
        public bool TransferItem(ItemSlot p_source, ItemSlot p_target)
        {
            return _transferModule != null &&
                   _transferModule.Transfer(p_source, p_target);
        }
    }
}
