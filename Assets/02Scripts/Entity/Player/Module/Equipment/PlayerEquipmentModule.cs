using System.Collections.Generic;
using UnityEngine;

public class PlayerEquipmentModule : MonoBehaviour
{
    private readonly List<SlotBase> _slotList = new();

    public IReadOnlyList<SlotBase> SlotList => _slotList;

    private void Awake()
    {
        _slotList.Add(new WeaponSlot(EWeaponType.Melee));
        _slotList.Add(new WeaponSlot(EWeaponType.Range));
        _slotList.Add(new WeaponSlot(EWeaponType.Special));

        _slotList.Add(new ArmorSlot(EArmorType.Helmet));
        _slotList.Add(new ArmorSlot(EArmorType.Chest));
        _slotList.Add(new ArmorSlot(EArmorType.Gloves));
        _slotList.Add(new ArmorSlot(EArmorType.Boots));
    }
}
