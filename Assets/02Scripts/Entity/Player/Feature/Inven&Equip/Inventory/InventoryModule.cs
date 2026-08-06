using UnityEngine;

namespace Alpha.Player.Inventory
{
    // Player Inventory 기능의 대표 진입점.
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
        private InventoryTransferModule _transferModule;

        // Unity 초기화 시 필요한 컴포넌트와 내부 객체를 준비한다.
        private void Awake()
        {
            // 슬롯 생성 조회 담당
            _slotModule = new CreateInventorySlotModule(_weaponSlotCount, _armorSlotCount, _commonSlotCount);

            // 드래그 앤 드랍 및 교환 담당
            _transferModule = new InventoryTransferModule();
        }

        // 슬롯을 생성하고 저장·이동 Module을 현재 Context에 연결한다.
        public bool Initialize(InventoryContext p_context)
        {
            if (p_context == null)
                return false;

            if (!_slotModule.Initialize(p_context))
                return false;

            _storageModule =
                new InventoryStorageModule(p_context);

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

        // 특정 슬롯이 요청 수량 전체를 받을 수 있는지 검사한다.
        public bool CanAddItem(InventorySlot p_slot, ItemDTO p_item, int p_count)
        {
            return p_slot != null && p_count > 0 && p_slot.GetAddableCount(p_item) >= p_count;
        }

        // 지정 슬롯에 아이템을 가능한 만큼 추가한다.
        public int AddItemToSlot(InventorySlot p_slot, ItemDTO p_item, int p_count)
        {
            if (p_slot == null || p_count <= 0)
                return 0;

            return p_slot.Add(p_item, p_count);
        }

        // 지정 슬롯에서 아이템을 가능한 만큼 제거한다.
        public int RemoveItem(InventorySlot p_slot, int p_count)
        {
            if (p_slot == null || p_count <= 0)
                return 0;

            return p_slot.Remove(p_count);
        }

        // 슬롯 전체를 새 아이템 상태로 교체할 수 있는지 검사한다.
        public bool CanReplaceItem(InventorySlot p_slot, ItemDTO p_item, int p_count)
        {
            return p_slot != null && p_slot.CanReplace(p_item, p_count);
        }

        // 슬롯 전체를 새 아이템과 수량으로 교체한다.
        public bool ReplaceItem(InventorySlot p_slot, ItemDTO p_item, int p_count)
        {
            return p_slot != null && p_slot.Replace(p_item, p_count);
        }

        // 두 슬롯의 상태에 따라 이동·병합·교환을 실행한다.
        public EInventoryTransferResult TransferItem(InventorySlot p_source, InventorySlot p_target)
        {
            if (_transferModule == null)
                return EInventoryTransferResult.Rejected;

            return _transferModule.Transfer(p_source, p_target);
        }
    }
}
