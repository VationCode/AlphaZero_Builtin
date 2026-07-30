using Alpha.Slot;
using UnityEngine;

namespace Alpha.Equipment
{
    public class EquipmentArmorPageView : MonoBehaviour
    {
        [SerializeField] private SlotViewBase _helmetSlot;
        [SerializeField] private SlotViewBase _chestSlot;
        [SerializeField] private SlotViewBase _glovesSlot;
        [SerializeField] private SlotViewBase _bootsSlot;

        public bool TryGetSlot(EArmorType p_type, out SlotViewBase p_slotView)
        {
            p_slotView = p_type switch
            {
                EArmorType.Helmet => _helmetSlot,
                EArmorType.Chest => _chestSlot,
                EArmorType.Gloves => _glovesSlot,
                EArmorType.Boots => _bootsSlot,
                _ => null
            };

            return p_slotView != null;
        }
    }
}
