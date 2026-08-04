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

        public void RequestAddSlot(EItemType p_itemType, int p_groupIndex)
        {
            // 현재 열린 페이지의 요청만 허용한다.
            if (!IsOpen || CurrentWindow != p_itemType)
                return;

            _module.AddSlot(p_itemType, p_groupIndex);
        }

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
    }
}
