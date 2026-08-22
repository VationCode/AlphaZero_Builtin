using UnityEngine;
using Alpha.Combat;
using Alpha.Item.Weapon;
using Alpha.Item.Weapon.Melee;
using Alpha.Item.Weapon.Range;
using System;

namespace Alpha.Player.Combat
{
    [RequireComponent(typeof(WeaponSwapModule))]
    [RequireComponent(typeof(RangeAimModule))]
    public class CombatModule : MonoBehaviour, IRangeAttackSource
    {
        private PlayerCore _core;
        private WeaponSwapModule _weaponSwapModule;
        private RangeAimModule _rangeAimModule;
        private RangeWeapon _activeRangeSecondaryWeapon;

        public Transform Attacker => _core?.PlayerTr;

        // 실제 전투에 적용된 활성 무기의 변경을 외부 표현 계층에 알린다.
        public event Action<WeaponDTO> OnWeaponChanged;

        // Player의 공격으로 피해가 실제 적용된 경우에만 명중 표현에 알린다.
        public event Action<DamageInfo> OnHitConfirmed;

        // 현재 전투에 사용 가능한 무기를 대표 진입점으로 제공한다.
        public Weapon CurrentWeapon => _weaponSwapModule?.CurrentWeapon;
        public RangeWeapon CurrentRangeWeapon => CurrentWeapon as RangeWeapon;
        public bool HasWeapon => CurrentWeapon != null;

        public EWeaponActionType ActiveActionType =>
            CurrentWeapon?.ActiveActionType ?? EWeaponActionType.None;

        public bool HasActiveAction =>
            CurrentWeapon != null && CurrentWeapon.HasActiveAction;

        public RangeWeapon ActiveRangeSecondaryWeapon =>
            _activeRangeSecondaryWeapon;

        public bool HasActiveRangeSecondary =>
            _activeRangeSecondaryWeapon != null &&
            _activeRangeSecondaryWeapon.IsSecondaryActive;

        private void Awake()
        {
            _weaponSwapModule = GetComponent<WeaponSwapModule>();
            _rangeAimModule = GetComponent<RangeAimModule>();
        }

        private void OnEnable()
        {
            SubscribeDamageApplied();
        }

        private void OnDisable()
        {
            UnsubscribeDamageApplied();
        }

        // Player 전투 기능과 런타임 무기 생성 의존성을 연결한다.
        public bool Bind(PlayerCore p_core)
        {
            if (p_core == null ||
                _weaponSwapModule == null ||
                _rangeAimModule == null ||
                !_weaponSwapModule.Bind(p_core.ResourceLoader) ||
                !_rangeAimModule.Bind(p_core))
            {
                Debug.LogError($"{nameof(CombatModule)}의 참조가 설정되지 않았습니다.", this);

                return false;
            }

            _core = p_core;
            SubscribeDamageApplied();
            return true;
        }

        private void SubscribeDamageApplied()
        {
            if (_core == null || !isActiveAndEnabled)
                return;

            DamageSystem.OnDamageApplied -= HandleDamageApplied;
            DamageSystem.OnDamageApplied += HandleDamageApplied;
        }

        private void UnsubscribeDamageApplied()
        {
            DamageSystem.OnDamageApplied -= HandleDamageApplied;
        }

        // 공용 피해 성공 중 Player 또는 Player 자식이 공격자인 경우만 명중으로 확정한다.
        private void HandleDamageApplied(
            Collider p_target,
            DamageInfo p_damageInfo)
        {
            Transform player = _core?.PlayerTr;
            Transform attacker = p_damageInfo.Attacker;

            if (player == null ||
                attacker == null ||
                (attacker != player &&
                 !attacker.IsChildOf(player)))
            {
                return;
            }

            OnHitConfirmed?.Invoke(p_damageInfo);
        }

        #region ============================== Weapon Swap
        // 공통 무기 교체 요청을 실제 무기 생성 Module에 전달한다.
        public bool ApplyWeaponChange(WeaponDTO p_weapon)
        {
            // 기존 무기가 교체되기 전에 진행 중인 행동을 정리한다.
            CancelRangeSecondary();
            CancelWeaponAction();

            if (!_weaponSwapModule.Apply(p_weapon))
                return false;

            MeleeWeapon meleeWeapon = CurrentWeapon as MeleeWeapon;

            if (meleeWeapon != null &&
                !meleeWeapon.BindAttackSource(Attacker))
            {
                _weaponSwapModule.Apply(null);
                OnWeaponChanged?.Invoke(null);
                return false;
            }

            RangeWeapon rangeWeapon = CurrentRangeWeapon;

            if (rangeWeapon != null &&
                !rangeWeapon.BindAttackSource(this))
            {
                // 잘못 구성된 Range 무기는 장착 상태로 남기지 않는다.
                _weaponSwapModule.Apply(null);
                OnWeaponChanged?.Invoke(null);
                return false;
            }

            OnWeaponChanged?.Invoke(CurrentWeapon?.Data);
            return true;
        }

        #endregion ============================== /Weapon Swap

        #region ============================== CombatAction
        // 현재 무기의 Action을 선택하고 행동을 시작한다.
        public bool TryBeginWeaponAction(EWeaponActionType p_type)
        {
            return CurrentWeapon != null &&
                   CurrentWeapon.TryBeginAction(p_type);
        }

        // 진행 중인 무기 행동을 갱신한다.
        public void TickWeaponAction(
            bool p_isInputHeld,
            bool p_isInputPressed,
            float p_deltaTime)
        {
            CurrentWeapon?.TickAction(
                p_isInputHeld,
                p_isInputPressed,
                p_deltaTime);
        }

        public void CancelWeaponAction()
        {
            CurrentWeapon?.CancelAction();
        }

        public bool TryGetAttackPose(
            Vector3 p_muzzleOrigin,
            float p_maxDistance,
            float p_defaultAimDistance,
            out Vector3 p_attackOrigin,
            out Vector3 p_targetPoint)
        {
            if (_rangeAimModule == null)
            {
                p_attackOrigin = Vector3.zero;
                p_targetPoint = Vector3.zero;
                return false;
            }

            return _rangeAimModule.TryResolveAttackPose(
                p_muzzleOrigin,
                p_maxDistance,
                p_defaultAimDistance,
                out p_attackOrigin,
                out p_targetPoint);
        }

        // 현재 RangeWeapon의 총구 기준 전체 조준 방향을 반환한다.
        public bool TryGetRangeAimDirection(
            out Vector3 p_direction)
        {
            p_direction = Vector3.zero;

            return CurrentRangeWeapon != null &&
                   CurrentRangeWeapon.TryGetAimDirection(
                       out p_direction);
        }

        // 현재 Range 조준점을 Player의 지상 회전 방향으로 변환한다.
        public bool TryGetRangeFacingDirection(
            out Vector3 p_direction)
        {
            p_direction = Vector3.zero;

            if (_core == null ||
                !TryGetRangeAimDirection(
                    out Vector3 attackDirection))
            {
                return false;
            }

            p_direction = Vector3.ProjectOnPlane(
                attackDirection,
                Vector3.up);

            if (p_direction.sqrMagnitude <= 0.0001f)
                return false;

            p_direction.Normalize();
            return true;
        }
        #endregion ============================== /CombatAction

        #region ============================== Range Secondary
        public bool BeginRangeSecondary()
        {
            RangeWeapon rangeWeapon = CurrentRangeWeapon;

            if (rangeWeapon == null ||
                _activeRangeSecondaryWeapon != null ||
                !rangeWeapon.BeginSecondary())
            {
                return false;
            }

            _activeRangeSecondaryWeapon = rangeWeapon;
            return true;
        }

        public void TickRangeSecondary(float p_deltaTime)
        {
            _activeRangeSecondaryWeapon?.TickSecondary(p_deltaTime);
        }

        public void EndRangeSecondary()
        {
            _activeRangeSecondaryWeapon?.EndSecondary();
            _activeRangeSecondaryWeapon = null;
        }

        public void CancelRangeSecondary()
        {
            _activeRangeSecondaryWeapon?.CancelSecondary();
            _activeRangeSecondaryWeapon = null;
        }
        #endregion ============================== /Range Secondary
    }
}
