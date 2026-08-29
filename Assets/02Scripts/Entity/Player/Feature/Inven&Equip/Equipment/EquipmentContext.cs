using System;
using System.Collections.Generic;

namespace Alpha.Player.Equipment
{
    // 현재 장착 상태 보관 및 조회
    public class EquipmentContext
    {
        private readonly Dictionary<EWeaponCategory, WeaponEquipmentSlot> _weaponSlotDict = new();

        private readonly Dictionary<EArmorType, ArmorEquipmentSlot> _armorSlotDict = new();

        private readonly List<EquipmentSlot> _slotList = new();

        public IReadOnlyList<EquipmentSlot> Slots => _slotList;

        public event Action<EquipmentSlot> OnSlotChanged;

        // 전달받은 값으로 초기 상태를 구성한다.
        public EquipmentContext()
        {
            Register(new WeaponEquipmentSlot(EWeaponCategory.Melee));
            Register(new WeaponEquipmentSlot(EWeaponCategory.Range));
            Register(new WeaponEquipmentSlot(EWeaponCategory.Special));

            Register(new ArmorEquipmentSlot(EArmorType.Helmet));
            Register(new ArmorEquipmentSlot(EArmorType.Chest));
            Register(new ArmorEquipmentSlot(EArmorType.Gloves));
            Register(new ArmorEquipmentSlot(EArmorType.Boots));
        }

        // TryGetWeaponSlot 조건을 검사하고 성공 여부와 결과를 반환한다.
        public bool TryGetWeaponSlot(
            EWeaponCategory p_weaponCategory,
            out WeaponEquipmentSlot p_slot)
        {
            return _weaponSlotDict.TryGetValue(p_weaponCategory, out p_slot);
        }

        // TryGetArmorSlot 조건을 검사하고 성공 여부와 결과를 반환한다.
        public bool TryGetArmorSlot(EArmorType p_armorType, out ArmorEquipmentSlot p_slot)
        {
            return _armorSlotDict.TryGetValue(p_armorType, out p_slot);
        }

        // 등록
        private void Register(WeaponEquipmentSlot p_slot)
        {
            _weaponSlotDict.Add(p_slot.WeaponCategory, p_slot);
            RegisterSlot(p_slot);
        }

        // Register 대상을 등록하고 변경 통지를 연결한다.
        private void Register(ArmorEquipmentSlot p_slot)
        {
            _armorSlotDict.Add(p_slot.ArmorType, p_slot);
            RegisterSlot(p_slot);
        }

        // RegisterSlot 대상을 등록하고 변경 통지를 연결한다.
        private void RegisterSlot(EquipmentSlot p_slot)
        {
            _slotList.Add(p_slot);
            p_slot.OnChanged += HandleSlotChanged;
        }

        // HandleSlotChanged 이벤트를 받아 필요한 후속 처리를 수행한다.
        private void HandleSlotChanged(EquipmentSlot p_slot)
        {
            OnSlotChanged?.Invoke(p_slot);
        }
    }
}
