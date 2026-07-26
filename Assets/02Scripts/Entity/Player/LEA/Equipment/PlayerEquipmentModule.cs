using Alpha.Player.Slot;
using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Player.Equipment
{
    public class PlayerEquipmentModule : MonoBehaviour
    {
        private readonly Dictionary<EWeaponType, WeaponSlot> _weaponSlots = new();

        private readonly Dictionary<EArmorType, ArmorSlot> _armorSlots = new();

        public bool IsInitialized { get; private set; }

        public void InitializeSlots()
        {
            if (IsInitialized)
                return;

            AddWeaponSlot(EWeaponType.Melee);
            AddWeaponSlot(EWeaponType.Range);
            AddWeaponSlot(EWeaponType.Special);

            AddArmorSlot(EArmorType.Helmet);
            AddArmorSlot(EArmorType.Chest);
            AddArmorSlot(EArmorType.Gloves);
            AddArmorSlot(EArmorType.Boots);

            IsInitialized = true;
        }

        private void AddWeaponSlot(EWeaponType p_type)
        {
            WeaponSlot slot = SlotFactory.CreateWeaponSlot(p_type);

            _weaponSlots.Add(p_type, slot);
        }

        private void AddArmorSlot(EArmorType p_type)
        {
            ArmorSlot slot = SlotFactory.CreateArmorSlot(p_type);

            _armorSlots.Add(p_type, slot);
        }

        public WeaponSlot GetWeaponSlot(EWeaponType p_type)
        {
            return _weaponSlots.TryGetValue(p_type, out WeaponSlot slot)? slot : null;
        }

        public ArmorSlot GetArmorSlot(EArmorType p_type)
        {
            return _armorSlots.TryGetValue(p_type, out ArmorSlot slot)? slot : null;
        }
    }
}