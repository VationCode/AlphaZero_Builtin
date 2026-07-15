using UnityEngine;

public class EquipmentView : MonoBehaviour
{
    [SerializeField]
    private WeaponEquipmentView _weaponView;

    [SerializeField]
    private ArmorEquipmentView _armorView;

    public WeaponEquipmentView WeaponView => _weaponView;
    public ArmorEquipmentView ArmorView => _armorView;
}
