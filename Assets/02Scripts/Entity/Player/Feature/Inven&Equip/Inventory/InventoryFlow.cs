using System;
using UnityEngine;

namespace Alpha.Player.Inventory
{
    // InventoryFlow 요청의 조건과 실행 순서를 결정한다.
    public class InventoryFlow : MonoBehaviour
    {
        private InventoryContext _context;
        private InventoryModule _module;
        private AlphaInputSystem _input;

        public bool IsOpen { get; private set; }
        public EItemType CurrentWindow { get; private set; } = EItemType.None;

        // View가 현재 화면 상태를 반영할 때 사용한다.
        public event Action<bool, EItemType> OnViewStateChanged;

        // 인벤토리 상태, 실행 Module, 입력을 연결한다.
        public bool Bind(InventoryContext p_context, InventoryModule p_module, AlphaInputSystem p_input)
        {
            if (p_context == null || p_module == null)
                return false;

            _context = p_context;
            _module = p_module;

            _input = p_input;

            return true;
        }

        // 매 프레임 입력과 현재 상태를 갱신한다.
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
        // 현재 열림 상태에 따라 인벤토리를 열거나 닫는다.
        private void ToggleInventory()
        {
            if (IsOpen)
            {
                RequestCloseInventory();
                return;
            }

            OpenCategory();
        }

        // 인벤토리를 카테고리 선택 화면으로 연다.
        private void OpenCategory()
        {
            IsOpen = true;
            CurrentWindow = EItemType.None;

            NotifyViewState();
        }

        // 열린 인벤토리의 표시 페이지를 요청한 아이템 종류로 전환한다.
        public void RequestOpenPage(EItemType p_itemType)
        {
            if (!IsOpen)
                return;

            CurrentWindow = p_itemType;

            NotifyViewState();
        }

        // 인벤토리를 닫고 선택된 페이지를 초기화한다.
        public void RequestCloseInventory()
        {
            if (!IsOpen)
                return;

            IsOpen = false;
            CurrentWindow = EItemType.None;

            NotifyViewState();
        }

        // 현재 페이지에서 카테고리 선택 화면으로 돌아간다.
        public void RequestBackToCategory()
        {
            if (!IsOpen)
                return;

            CurrentWindow = EItemType.None;

            NotifyViewState();
        }

        // NotifyViewState 변경 사실을 구독자에게 알린다.
        private void NotifyViewState()
        {
            OnViewStateChanged?.Invoke(IsOpen, CurrentWindow);
        }
        #endregion ============================== /창 관리

        #region ============================== Drag & Drop 관리
        // View로부터의 이벤트 요청이 들어왔을 때 로직에서의 인벤토리 처리(Move, Merge, Swap)
        public void RequestTransferItem(int p_sourceSlotIndex, int p_targetSlotIndex)
        {
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
