using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEquipmentModule : MonoBehaviour
{
    private readonly List<SlotBase> _slotList = new();

    public IReadOnlyList<SlotBase> SlotList => _slotList;
    public WeaponDTO ActiveWeapon => GetEquippedWeapon(ActiveWeaponType);
    public EWeaponType ActiveWeaponType { get; private set; }= EWeaponType.None;

    public event Action<SlotBase> EquipmentChanged;
    public event Action<WeaponDTO> ActiveWeaponChanged;
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
        WeaponSlot weaponSlot = p_slot as WeaponSlot;

        bool wasActiveWeapon = weaponSlot != null && weaponSlot.WeaponType == ActiveWeaponType;

        EquipmentChanged?.Invoke(p_slot);

        if (!wasActiveWeapon) return;

        // EquipmentChanged 처리 도중 다른 무기로 변경됐을 수 있다.
        if (weaponSlot.WeaponType != ActiveWeaponType) return;

        ActiveWeaponChanged?.Invoke(ActiveWeapon);
    }

    #region ==================== Weapon
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
    public WeaponSlot GetWeaponSlot(EWeaponType p_weaponType)
    {
        foreach (SlotBase slot in _slotList)
        {
            if (slot is not WeaponSlot weaponSlot)
                continue;

            if (weaponSlot.WeaponType == p_weaponType)
                return weaponSlot;
        }

        return null;
    }
    public bool TrySelectWeapon(EWeaponType p_weaponType)
    {
        if (p_weaponType == EWeaponType.None)
            return false;

        WeaponSlot weaponSlot = GetWeaponSlot(p_weaponType);

        if (weaponSlot == null || weaponSlot.IsEmpty)
            return false;

        if (ActiveWeaponType == p_weaponType)
            return true;

        ActiveWeaponType = p_weaponType;
        ActiveWeaponChanged?.Invoke(ActiveWeapon);

        return true;
    }
    public void ClearActiveWeapon()
    {
        if (ActiveWeaponType == EWeaponType.None)
            return;

        ActiveWeaponType = EWeaponType.None;
        ActiveWeaponChanged?.Invoke(null);
    }
    #endregion ==================== /Weapon


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
