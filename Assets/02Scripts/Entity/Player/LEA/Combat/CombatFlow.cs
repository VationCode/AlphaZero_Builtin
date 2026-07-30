using Alpha.Equipment;
using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Player.Combat
{
    // Player의 전투 행동 판단과 무기 상태 동기화를 담당한다.
    public class CombatFlow : MonoBehaviour
    {
        private PlayerCore _core;

        private readonly Dictionary<ECombatStateType, CombatStateBase> _stateDict = new();

        public CombatStateBase CurrentState { get; private set; }
        public bool IsBound { get; private set; }

        public void Bind(PlayerCore p_core)
        {
            if (p_core == null || p_core.EquipmentModule == null || 
                !p_core.EquipmentModule.IsBound || p_core.CombatModule == null||
                !p_core.CombatModule.IsBound)
            {
                Debug.LogError($"{nameof(CombatFlow)}의 참조가 설정되지 않았습니다.", this);
                return;
            }

            // 재연결 시 이전 활성 무기 이벤트를 해제한다.
            if (_core?.EquipmentModule != null)
            {
                _core.EquipmentModule.OnActiveWeaponChanged -= HandleActiveWeaponChanged;
            }

            _core = p_core;

            _core.EquipmentModule.OnActiveWeaponChanged += HandleActiveWeaponChanged;

            InitializeStates();

            IsBound = EnterFlow(ECombatStateType.Idle);
        }

        private void InitializeStates()
        {
            ExitFlow();
            _stateDict.Clear();

            RegisterState(new CombatIdleState(_core, this));
            RegisterState(new WeaponSwapState(_core, this));
            RegisterState(new AttackState(_core, this));
        }


        private void Update()
        {
            if (!IsBound) return;

            TickFlow();
            UpdateAimState();
        }

        // 장비
        /// <summary>
        /// 활성 무기가 변경되면 Combat에서 사용할 기본 공격만 갱신한다.
        /// 무기 외형과 Animator 처리는 PlayerEquipmentModule이 담당한다.
        /// </summary>
        private void HandleActiveWeaponChanged(WeaponDTO p_weapon)
        {
            AttackDefinition basicAttack = CreateBasicAttackDefinition(p_weapon);

            _core.CombatContext.SetBasicAttack(basicAttack);
        }

        // 상태
        // // 상태 등록
        internal bool RegisterState(CombatStateBase p_state)
        {
            if (p_state == null || _stateDict.ContainsKey(p_state.Type))
            {
                return false;
            }

            _stateDict.Add(p_state.Type, p_state);

            return true;
        }

        internal bool EnterFlow(ECombatStateType p_entryState)
        {
            return TryChangeState(p_entryState);
        }

        internal void TickFlow()
        {
            CurrentState?.TickState();
        }

        internal void ExitFlow()
        {
            CurrentState?.ExitState();
            CurrentState = null;
        }

        // 상태 변경
        internal bool TryChangeState(ECombatStateType p_nextState)
        {
            if (!_stateDict.TryGetValue(p_nextState, out CombatStateBase nextState))
            {
                return false;
            }

            if (ReferenceEquals(CurrentState, nextState))
                return false;

            CurrentState?.ExitState();

            CurrentState = nextState;
            _core.CombatContext.CurrentState = nextState.Type;

            CurrentState.EnterState();

            return true;
        }

        #region ======================================== 무기 변경관련 순서
        // 1. TryRequestWeaponSwap로 입력값 검증
        // 대상 무기 종류를 PendingWeaponType에 저장
        // 아직 실제 무기는 변경하지 않음
        internal bool TryRequestWeaponSwap(int p_slotIndex)
        {
            if (!IsBound || _core.BlockCombat || CurrentState?.Type != ECombatStateType.Idle)
            {
                return false;
            }

            if (!CanWeaponSwap())
                return false;

            return _core.CombatModule.TryPrepareWeaponSwap(p_slotIndex);
        }

        // 2. 미리 저장된 무기 교체 요청을 실제 무기 변경으로 확정
        // WeaponSwapState.Enter()에서 호출
        // 요청한 무기가 여전히 장착돼 있는지 재검증
        // TryApplyWeapon()을 통해 실제 상태와 프리팹 변경
        internal bool TryExecutePendingWeaponSwap()
        {
            return IsBound && _core.CombatModule.TryExecutePendingWeaponSwap();
        }

        private bool CanWeaponSwap()
        {
            if (_core.BlockCombat)
                return false;

            return _core.LocomotionContext.CurrentState switch
            {
                ELocoStateType.Dash => false,
                ELocoStateType.Jump => false,
                ELocoStateType.Die => false,
                _ => true
            };
        }
        #endregion ======================================== /무기 변경관련 순서

        internal bool CanStartAttack()
        {
            if (!IsBound || _core.BlockCombat || 
                CurrentState?.Type != ECombatStateType.Idle)
            {
                return false;
            }

            return _core.LocomotionContext.CurrentState switch
            {
                ELocoStateType.Jump => false,
                ELocoStateType.Fall => false,
                ELocoStateType.Rising => false,
                ELocoStateType.Dash => false,
                ELocoStateType.Die => false,
                _ => true
            };
        }

        #region ======================================== Aim
        // Player의 현재 조건을 판단해 Camera Aim View를 전환한다.
        private void UpdateAimState()
        {
            bool shouldAim =
                !_core.BlockCombat && _core.Input != null &&
                _core.Input.IsAiming &&
                CurrentState?.Type != ECombatStateType.WeaponSwap;

            if (_core.CombatContext.IsAiming == shouldAim)
                return;

            if (_core.CameraCore == null || !_core.CameraCore.TrySetAim(shouldAim))
            {
                return;
            }

            // Camera 전환에 성공한 뒤 Player Aim 상태를 확정한다.
            _core.CombatContext.SetAiming(shouldAim);
        }
        #endregion ======================================== /Aim

        #region ======================================== Attack
        // 장착 무기에 대한 기본 공격 정책을 만든다.
        private static AttackDefinition CreateBasicAttackDefinition(WeaponDTO p_weapon)
        {
            switch (p_weapon)
            {
                case MeleeWeaponDTO:
                    return new AttackDefinition
                    {
                        AnimationKey = "BasicAttack",
                        MovePolicy = EAttackMovePolicy.AnimationDriven,
                        AnimationPolicy = EAttackAnimationPolicy.FullBody,
                        RotationPolicy = EAttackRotationPolicy.TrackAim,
                        DeliveryType = EAttackDeliveryType.MeleeHitbox,

                        // 근접 공격 종료는 이후 애니메이션 이벤트로 판단한다.
                        Cooldown = 0f
                    };

                case RangeWeaponDTO rangeWeapon:
                    return new AttackDefinition
                    {
                        AnimationKey = "BasicAttack",
                        MovePolicy = EAttackMovePolicy.Free,
                        AnimationPolicy = EAttackAnimationPolicy.UpperBody,
                        RotationPolicy = EAttackRotationPolicy.TrackAim,
                        DeliveryType = EAttackDeliveryType.HitScan,

                        // 현재는 Rate를 공격 간격(초)으로 사용한다.
                        Cooldown = Mathf.Max(0f, rangeWeapon.Rate)
                    };

                default:
                    return null;
            }
        }

        internal bool TryPrepareBasicAttack()
        {
            return _core.CombatContext.TryActivateBasicAttack();
        }
        #endregion ======================================== /Attack

        private void OnDestroy()
        {
            if (_core?.EquipmentModule != null)
            {
                _core.EquipmentModule.OnActiveWeaponChanged -= HandleActiveWeaponChanged;
            }
        }
    }
}
