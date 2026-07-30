using Alpha.Player.Animation;
using System;
using UnityEngine;

namespace Alpha.Player.Equipment
{
    /// <summary>
    /// Player의 현재 무기 상태와 장비 외형을 실제로 적용한다.
    /// 장비를 언제 변경할지는 PlayerEquipmentFlow와 CombatFlow가 판단한다.
    /// </summary>
    public class PlayerEquipmentModule : MonoBehaviour
    {
        private PlayerEquipmentContext _context;
        private ResourceLoadSystem _resourceLoader;
        private PlayerEquipmentView _equipmentView;
        private PlayerAnimationView _animationView;

        // 현재 무기 조회 속성
        public WeaponDTO CurrentWeapon => _context?.CurrentWeapon;
        public EWeaponType CurrentWeaponType => _context?.CurrentWeaponType ?? EWeaponType.None;

        public bool IsBound { get; private set; }

        /// <summary>
        /// Player가 실제로 사용하는 무기가 변경됐을 때 전달한다.
        /// CombatFlow는 이 이벤트를 통해 공격 정보를 갱신할 수 있다.
        /// </summary>
        public event Action<WeaponDTO> OnActiveWeaponChanged;

        public bool Bind(PlayerEquipmentContext p_context, ResourceLoadSystem p_resourceLoader,
                         PlayerEquipmentView p_equipmentView, PlayerAnimationView p_animationView)
        {
            if (p_context == null || p_resourceLoader == null ||
                p_equipmentView == null || p_animationView == null)
            {
                Debug.LogError($"{nameof(PlayerEquipmentModule)}의 참조가 설정되지 않았습니다.", this);
                return false;
            }

            _context = p_context;
            _resourceLoader = p_resourceLoader;
            _equipmentView = p_equipmentView;
            _animationView = p_animationView;

            IsBound = true;
            return true;
        }

        #region ============================== Weapon
        /// <summary>
        /// 선택된 무기를 Player의 현재 무기와 외형에 적용한다.
        /// 외형 적용에 실패하면 이전 무기 상태로 복구한다.
        /// </summary>
        public bool TryApplyWeapon(WeaponDTO p_weapon)
        {
            if (!IsBound || p_weapon == null)
                return false;

            GameObject prefab = _resourceLoader.GetItemPrefab(p_weapon.ItemType, p_weapon.PrefabKey);

            if (prefab == null)
                return false;

            WeaponDTO previousWeapon = _context.CurrentWeapon;

            if (!_context.TrySetWeapon(p_weapon))
                return false;

            if (!_equipmentView.TryShowWeapon(prefab))
            {
                // 외형 적용 실패 시 현재 무기 상태를 이전 값으로 복구한다.
                RestoreWeapon(previousWeapon);
                return false;
            }

            _animationView.ApplyWeaponOverrideController(p_weapon.WeaponType);

            OnActiveWeaponChanged?.Invoke(p_weapon);
            return true;
        }

        public bool TryClearWeapon()
        {
            if (!IsBound || !_context.TryClearWeapon())
            {
                return false;
            }

            // 외형이 이미 없더라도 Player의 무기 상태는 정상적으로 해제한다.
            _equipmentView.TryClearWeapon();

            _animationView.ApplyWeaponOverrideController(EWeaponType.None);

            OnActiveWeaponChanged?.Invoke(null);
            return true;
        }

        private void RestoreWeapon(WeaponDTO p_previousWeapon)
        {
            if (p_previousWeapon == null)
            {
                _context.TryClearWeapon();
                return;
            }

            _context.TrySetWeapon(p_previousWeapon);
        }

        #endregion ============================== /Weapon

        #region Armor
        public bool TryApplyArmor(EArmorType p_type, ArmorDTO p_armor)
        {
            if (!IsBound || p_armor == null || p_armor.ArmorType != p_type)
            {
                return false;
            }

            GameObject prefab =
                _resourceLoader.GetItemPrefab(p_armor.ItemType, p_armor.PrefabKey);

            if (prefab == null)
                return false;

            return _equipmentView.TryShowArmor(p_type, prefab);
        }

        public bool TryClearArmor(EArmorType p_type)
        {
            return IsBound && _equipmentView.TryClearArmor(p_type);
        }
        #endregion ============================== /Armor

        public void Unbind()
        {
            _context = null;
            _resourceLoader = null;
            _equipmentView = null;
            _animationView = null;

            IsBound = false;
            OnActiveWeaponChanged = null;
        }

        private void OnDestroy()
        {
            Unbind();
        }
    }
}

/*
PlayerEquipmentModule
├─ 현재 사용 무기 상태 변경
├─ 무기 프리팹 표시와 제거
├─ 무기 Animator Override 적용
├─ 방어구 프리팹 표시와 제거
└─ 무기 외형 실패 시 상태 복구
*/