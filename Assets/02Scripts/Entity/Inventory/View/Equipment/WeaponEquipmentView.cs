using UnityEngine;

public class WeaponEquipmentView : MonoBehaviour
{
    [Header("Weapon Slots")]
    [SerializeField] private SlotBaseUI _meleeSlot;
    [SerializeField] private SlotBaseUI _rangeSlot;
    [SerializeField] private SlotBaseUI _specialSlot;



    public SlotBaseUI GetWeaponSlot(EWeaponType p_weaponType)
    {
        switch (p_weaponType)
        {
            case EWeaponType.Melee:
                return _meleeSlot;

            case EWeaponType.Range:
                return _rangeSlot;

            case EWeaponType.Special:
                return _specialSlot;

            default:
                return null;
        }
    }
}
