using Alpha.UI.Equipment;
using Alpha.UI.Inventory;
using UnityEngine;

namespace Alpha.UI
{
    public class UIManager : MonoBehaviour
    {
        public CrossHairUI CrossHairUI;
        public StateUI StateUI;
        public PlayerInventoryView InventoryView;
        public PlayerEquipmentView EquipmentView;

        private void Awake()
        {
            InventoryView = GetComponentInChildren<PlayerInventoryView>(true);
            EquipmentView = GetComponentInChildren<PlayerEquipmentView>(true);
        }
    }
}