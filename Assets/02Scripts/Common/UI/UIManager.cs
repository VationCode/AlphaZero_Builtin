using Alpha.UI.Inventory;
using UnityEngine;

namespace Alpha.UI
{
    public class UIManager : MonoBehaviour
    {
        public CrossHairUI CrossHairUI;
        public StateUI StateUI;
        public PlayerInventoryView InventoryView;

        private void Awake()
        {
            InventoryView = GetComponentInChildren<PlayerInventoryView>();
        }
    }
}