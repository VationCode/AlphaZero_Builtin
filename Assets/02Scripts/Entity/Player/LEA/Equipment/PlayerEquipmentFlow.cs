using UnityEngine;


namespace Alpha.Player
{
    public class PlayerEquipmentFlow : MonoBehaviour
    {
        private PlayerEquipmentModule _equipmentModule;
        private PlayerEquipmentView _equipmentView;
        private ResourceLoadSystem _resourceLoader;
        private PlayerAnimationView _animationView;

        public void Bind(PlayerCore p_core)
        {
            Unbind();

            _equipmentModule = p_core.EquipmentModule;
            _equipmentView = p_core.EquipmentView;
            _animationView = p_core.AnimationView;
            _resourceLoader = p_core.ResourceLoader;

            if (_equipmentModule == null || _equipmentView == null || _resourceLoader == null)
            {
                Debug.LogError(
                    "[PlayerEquipmentFlow] 필수 의존성이 연결되지 않았습니다.",
                    this);

                return;
            }

            _equipmentModule.EquipmentChanged += OnEquipmentChanged;
            _equipmentModule.ActiveWeaponChanged += OnActiveWeaponChanged;

            // 시작 시 현재 무기 상태에 맞는 Controller만 적용한다.
            // 여기에서는 Swap 애니메이션을 실행하지 않는다.
            _animationView?.ApplyWeaponOverrideController(_equipmentModule.ActiveWeaponType);

            foreach (SlotBase slot in _equipmentModule.SlotList)
            {
                OnEquipmentChanged(slot);
            }
        }

        public bool TrySelectWeapon(int p_swapNum)
        {
            EWeaponType weaponType;

            switch (p_swapNum)
            {
                case 0:
                    weaponType = EWeaponType.Melee;
                    break;

                case 1:
                    weaponType = EWeaponType.Range;
                    break;

                case 2:
                    weaponType = EWeaponType.Special;
                    break;

                default:
                    return false;
            }

            return SelectWeapon(weaponType);
        }
        private bool SelectWeapon(EWeaponType p_weaponType)
        {
            if (_equipmentModule.ActiveWeaponType == p_weaponType)
                return false;

            WeaponSlot weaponSlot = _equipmentModule.GetWeaponSlot(p_weaponType);

            if (weaponSlot == null || weaponSlot.IsEmpty)
                return false;

            if (!TryEquipSlot(weaponSlot))
                return false;

            if (!_equipmentModule.TrySelectWeapon(p_weaponType))
                return false;

            return true;
        }
        private void OnActiveWeaponChanged(WeaponDTO p_weapon)
        {
            EWeaponType weaponType = p_weapon != null ? p_weapon.WeaponType : EWeaponType.None;

            _animationView?.ApplyWeaponOverrideController(weaponType);
            _animationView?.PlayWeaponSwap();
        }

        private void OnEquipmentChanged(SlotBase p_slot)
        {
            if (p_slot == null)
                return;

            if (p_slot is ArmorSlot armorSlot)
            {
                SynchronizeArmor(armorSlot);
                return;
            }

            if (p_slot is not WeaponSlot weaponSlot)
                return;

            if (weaponSlot.IsEmpty)
            {
                OnWeaponRemoved(weaponSlot);
                return;
            }

            // 첫 무기 장착
            if (_equipmentModule.ActiveWeaponType == EWeaponType.None)
            {
                SelectWeapon(weaponSlot.WeaponType);
                return;
            }

            // 현재 활성화된 무기 슬롯의 장비 교체
            if (_equipmentModule.ActiveWeaponType == weaponSlot.WeaponType)
            {
                TryEquipSlot(weaponSlot);
            }
        }

        private void SynchronizeArmor(SlotBase p_slot)
        {
            if (p_slot.IsEmpty)
            {
                _equipmentView.Unequip(p_slot);
                return;
            }

            TryEquipSlot(p_slot);
        }

        private void OnWeaponRemoved(WeaponSlot p_weaponSlot)
        {
            // 비활성 무기 슬롯에서 제거된 경우 현재 무기에는 영향이 없다.
            if (_equipmentModule.ActiveWeaponType != p_weaponSlot.WeaponType)
                return;

            _equipmentView.Unequip(p_weaponSlot);

            // 대체 무기가 선택되면 ActiveWeaponChanged가 발생한다.
            if (TrySelectFallbackWeapon()) return;

            // 남은 무기가 없으면 null로 ActiveWeaponChanged가 발생한다.
            _equipmentModule.ClearActiveWeapon();
        }

        private bool TrySelectFallbackWeapon()
        {
            foreach (SlotBase slot in _equipmentModule.SlotList)
            {
                if (slot is not WeaponSlot weaponSlot ||
                    weaponSlot.IsEmpty)
                {
                    continue;
                }

                if (SelectWeapon(weaponSlot.WeaponType))
                    return true;
            }

            return false;
        }

        private bool TryEquipSlot(SlotBase p_slot)
        {
            if (p_slot == null || p_slot.IsEmpty)
                return false;

            ItemDTO item = p_slot.Item;

            GameObject prefab = _resourceLoader.GetItemPrefab(item.ItemType, item.PrefabKey);

            if (prefab == null)
                return false;

            return _equipmentView.Equip(p_slot, prefab);
        }

        private void Unbind()
        {
            if (_equipmentModule == null) return;

            _equipmentModule.EquipmentChanged -= OnEquipmentChanged;
            _equipmentModule.ActiveWeaponChanged -= OnActiveWeaponChanged;
        }

        private void OnDestroy()
        {
            Unbind();
        }
    }
}