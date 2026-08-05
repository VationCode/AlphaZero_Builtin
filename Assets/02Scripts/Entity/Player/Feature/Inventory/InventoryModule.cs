using UnityEngine;
using System;

namespace Alpha.Player.Inventory
{
    // Player의 Inventory를 관리하는 모듈이다.
    public class InventoryModule : MonoBehaviour
    {
        [Header("Creat Slot")]
        [SerializeField, Min(0)] private int _weaponSlotCount = 10;
        [SerializeField, Min(0)] private int _armorSlotCount = 10;
        [SerializeField, Min(0)] private int _commonSlotCount = 10;

        // 기능만 담당할뿐 실제 정보는 Context에 저장
        private CreateInventorySlotModule _slotModule;      // 슬롯 생성, 확장 처리
        private InventoryStorageModule _storageModule;      // 아이템 정보 저장 처리
        private InventoryTransferModule _transferModule;    // Drag & Drop 처리
        private void Awake()
        {
            _slotModule = new CreateInventorySlotModule(_weaponSlotCount, _armorSlotCount, _commonSlotCount);
            _transferModule = new InventoryTransferModule();
        }

        public bool Initialize(InventoryContext p_context)
        {
            if (p_context == null)
                return false;

            // 슬롯 상태를 먼저 구성한다.
            if (!_slotModule.Initialize(p_context)) return false;

            _storageModule = new InventoryStorageModule(p_context);

            return true;
        }

        // 버튼에 의한 슬롯 추가
        public bool AddSlot(EItemType p_itemType, int p_groupIndex)
        {
            if (_slotModule == null)
                return false;

            return _slotModule.AddSlot(p_itemType, p_groupIndex);
        }

        // 아이템 픽업 시 아이템 저장
        public int AddItem(ItemDTO p_item, int p_count)
        {
            if (_storageModule == null)
                return 0;

            return _storageModule.AddItem(p_item, p_count);
        }

        // 
        public EInventoryTransferResult TransferItem(InventorySlot p_source, InventorySlot p_target)
        {
            if (_transferModule == null)
                return EInventoryTransferResult.Rejected;

            return _transferModule.Transfer(p_source, p_target);
        }
    }
}