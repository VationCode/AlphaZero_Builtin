using System;
using Alpha.UI;
using UnityEngine;

namespace Alpha.Player.Inventory
{
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
