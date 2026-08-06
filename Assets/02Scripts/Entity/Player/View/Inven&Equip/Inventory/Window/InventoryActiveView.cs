using System;
using Alpha.UI;
using UnityEngine;

namespace Alpha.Player.Inventory
{
    // 특정 아이템 종류의 인벤토리 창을 열고 닫는 표시 책임을 담당한다.
    public class InventoryActiveView : ViewBase
    {
        public EItemType ItemType => _itemType;
        [SerializeField] private EItemType _itemType;

        // Flow의 상태를 받은 InventoryView가 호출한다.
        internal void ApplyActive(bool p_isActive)
        {
            if (IsOpen == p_isActive)
                return;

            if (p_isActive)
                Open();
            else
                Close();
        }
    }
}
