using System;
using System.Collections.Generic;

namespace Alpha.Player.Equipment
{
    public class EquipmentContext
    {
        private readonly Dictionary<EWeaponType, WeaponEquipmentSlot> _weaponSlotDict = new();

        private readonly Dictionary<EArmorType, ArmorEquipmentSlot> _armorSlotDict = new();

        private readonly List<EquipmentSlot> _slotList = new();

        public IReadOnlyList<EquipmentSlot> Slots => _slotList;

        public event Action<EquipmentSlot> OnSlotChanged;

        public EquipmentContext()
        {
            Register(new WeaponEquipmentSlot(EWeaponType.Melee));
            Register(new WeaponEquipmentSlot(EWeaponType.Range));
            Register(new WeaponEquipmentSlot(EWeaponType.Special));

            Register(new ArmorEquipmentSlot(EArmorType.Helmet));
            Register(new ArmorEquipmentSlot(EArmorType.Chest));
            Register(new ArmorEquipmentSlot(EArmorType.Gloves));
            Register(new ArmorEquipmentSlot(EArmorType.Boots));
        }

        public bool TryGetWeaponSlot(EWeaponType p_weaponType, out WeaponEquipmentSlot p_slot)
        {
            return _weaponSlotDict.TryGetValue(p_weaponType, out p_slot);
        }

        public bool TryGetArmorSlot(EArmorType p_armorType, out ArmorEquipmentSlot p_slot)
        {
            return _armorSlotDict.TryGetValue(p_armorType, out p_slot);
        }

        // 등록
        private void Register(WeaponEquipmentSlot p_slot)
        {
            _weaponSlotDict.Add(p_slot.WeaponType, p_slot);
            RegisterSlot(p_slot);
        }

        private void Register(ArmorEquipmentSlot p_slot)
        {
            _armorSlotDict.Add(p_slot.ArmorType, p_slot);
            RegisterSlot(p_slot);
        }

        private void RegisterSlot(EquipmentSlot p_slot)
        {
            _slotList.Add(p_slot);
            p_slot.OnChanged += HandleSlotChanged;
        }

        private void HandleSlotChanged(EquipmentSlot p_slot)
        {
            OnSlotChanged?.Invoke(p_slot);
        }
    }
}
