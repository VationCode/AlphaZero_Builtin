using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEquipmentModule : MonoBehaviour
{
    private readonly List<SlotBase> _slotList = new();

    public IReadOnlyList<SlotBase> SlotList => _slotList;

    public event Action<SlotBase> EquipmentChanged;

    private void Awake()
    {
        AddSlot(new WeaponSlot(EWeaponType.Melee));
        AddSlot(new WeaponSlot(EWeaponType.Range));
        AddSlot(new WeaponSlot(EWeaponType.Special));

        AddSlot(new ArmorSlot(EArmorType.Helmet));
        AddSlot(new ArmorSlot(EArmorType.Chest));
        AddSlot(new ArmorSlot(EArmorType.Gloves));
        AddSlot(new ArmorSlot(EArmorType.Boots));
    }

    private void AddSlot(SlotBase p_slot)
    {
        _slotList.Add(p_slot);
        p_slot.Changed += OnSlotChanged;
    }

    private void OnSlotChanged(SlotBase p_slot)
    {
        EquipmentChanged?.Invoke(p_slot);
    }

    public WeaponDTO GetEquippedWeapon(EWeaponType p_weaponType)
    {
        foreach (SlotBase slot in _slotList)
        {
            if (slot is not WeaponSlot weaponSlot)
                continue;

            if (weaponSlot.WeaponType != p_weaponType)
                continue;

            return weaponSlot.Item as WeaponDTO;
        }

        return null;
    }

    public ArmorDTO GetEquippedArmor(EArmorType p_armorType)
    {
        foreach (SlotBase slot in _slotList)
        {
            if (slot is not ArmorSlot armorSlot)
                continue;

            if (armorSlot.ArmorType != p_armorType)
                continue;

            return armorSlot.Item as ArmorDTO;
        }

        return null;
    }
}
