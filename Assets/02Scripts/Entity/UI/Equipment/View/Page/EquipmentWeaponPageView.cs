using Alpha.Slot;
using UnityEngine;

namespace Alpha.Equipment
{
    public class EquipmentWeaponPageView : MonoBehaviour
    {
        [SerializeField] private SlotViewBase _meleeSlot;
        [SerializeField] private SlotViewBase _rangeSlot;
        [SerializeField] private SlotViewBase _specialSlot;

        public bool TryGetSlot(EWeaponType p_type, out SlotViewBase p_slotView)
        {
            p_slotView = p_type switch
            {
                EWeaponType.Melee => _meleeSlot,
                EWeaponType.Range => _rangeSlot,
                EWeaponType.Special => _specialSlot,
                _ => null
            };

            return p_slotView != null;
        }
    }
}
