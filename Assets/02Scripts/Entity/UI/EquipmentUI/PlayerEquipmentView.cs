using UnityEngine;

namespace Alpha.UI.Equipment
{
    public class PlayerEquipmentView : MonoBehaviour
    {
        [Header("Weapon")]
        [SerializeField] private SlotViewBase _melee;
        [SerializeField] private SlotViewBase _range;
        [SerializeField] private SlotViewBase _special;

        [Header("Armor")]
        [SerializeField] private SlotViewBase _helmet;
        [SerializeField] private SlotViewBase _chest;
        [SerializeField] private SlotViewBase _gloves;
        [SerializeField] private SlotViewBase _boots;

        public SlotViewBase GetWeaponView(EWeaponType p_type)
        {
            return p_type switch
            {
                EWeaponType.Melee => _melee,
                EWeaponType.Range => _range,
                EWeaponType.Special => _special,
                _ => null
            };
        }

        public SlotViewBase GetArmorView(EArmorType p_type)
        {
            return p_type switch
            {
                EArmorType.Helmet => _helmet,
                EArmorType.Chest => _chest,
                EArmorType.Gloves => _gloves,
                EArmorType.Boots => _boots,
                _ => null
            };
        }
    }
}