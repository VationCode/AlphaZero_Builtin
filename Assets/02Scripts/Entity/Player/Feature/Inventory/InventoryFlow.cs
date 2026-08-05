using System;
using UnityEngine;

namespace Alpha.Player.Inventory
{
    public class InventoryFlow : MonoBehaviour
    {
        private InventoryContext _context;
        private InventoryModule _module;
        private AlphaInputSystem _input;

        public bool IsOpen { get; private set; }
        public EItemType CurrentWindow { get; private set; } = EItemType.None;

        // View가 현재 화면 상태를 반영할 때 사용한다.
        public event Action<bool, EItemType> OnViewStateChanged;

        public bool Bind(InventoryContext p_context, InventoryModule p_module, AlphaInputSystem p_input)
        {
            if (p_context == null || p_module == null)
                return false;

            _context = p_context;
            _module = p_module;

            _input = p_input;

            return true;
        }

        private void Update()
        {
            if (_input != null && _input.IsInventory)
            {
                ToggleInventory();
            }
        }

        #region ============================== Slot 관리
        // 요청
        public void RequestAddSlot(EItemType p_itemType, int p_groupIndex)
        {
            // 현재 열린 페이지의 요청만 허용한다.
            if (!IsOpen || CurrentWindow != p_itemType)
                return;

            _module.AddSlot(p_itemType, p_groupIndex);
        }
        #endregion ============================== /Slot 관리

        #region ============================== 창 관리
        private void ToggleInventory()
        {
            if (IsOpen)
            {
                RequestCloseInventory();
                return;
            }

            OpenCategory();
        }

        private void OpenCategory()
        {
            IsOpen = true;
            CurrentWindow = EItemType.None;

            NotifyViewState();
        }

        public void RequestOpenPage(EItemType p_itemType)
        {
            if (!IsOpen)
                return;

            CurrentWindow = p_itemType;

            NotifyViewState();
        }

        public void RequestCloseInventory()
        {
            if (!IsOpen)
                return;

            IsOpen = false;
            CurrentWindow = EItemType.None;

            NotifyViewState();
        }

        public void RequestBackToCategory()
        {
            if (!IsOpen)
                return;

            CurrentWindow = EItemType.None;

            NotifyViewState();
        }

        private void NotifyViewState()
        {
            OnViewStateChanged?.Invoke(IsOpen, CurrentWindow);
        }
        #endregion ============================== /창 관리

        #region ============================== Drag & Drop 관리
        // View로부터의 이벤트 요청이 들어왔을 때 로직에서의 인벤토리 처리(Move, Merge, Swap)
        public void RequestTransferItem(int p_sourceSlotIndex, int p_targetSlotIndex)
        {
            // 인벤토리가 열린 상태에서만 허용
            if (!IsOpen || _context == null || _module == null)
            {
                return;
            }

            // SlotIndex로 (sourceSlot, targetSlot)Domain 조회 
            if (!_context.TryGetSlot(p_sourceSlotIndex, out InventorySlot sourceSlot))
            {
                return;
            }

            if (!_context.TryGetSlot(p_targetSlotIndex, out InventorySlot targetSlot))
            {
                return;
            }

            _module.TransferItem(sourceSlot, targetSlot);
        }
        #endregion ============================== /Drag & Drop 관리
    }
}
