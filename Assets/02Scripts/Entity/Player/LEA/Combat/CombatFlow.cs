using Alpha.Equipment;
using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Player.Combat
{
    // Player의 전투 행동 판단과 무기 상태 동기화를 담당한다.
    public class CombatFlow : MonoBehaviour
    {
        private PlayerCore _core;
        private EquipmentCore _equipmentCore;
        private ResourceLoadSystem _resourceLoader;

        private readonly Dictionary<ECombatStateType, CombatStateBase> _stateDict = new();

        public CombatStateBase CurrentState { get; private set; }
        public bool IsBound { get; private set; }

        public void Bind(PlayerCore p_core, EquipmentCore p_equipmentCore, ResourceLoadSystem p_resourceLoader)
        {
            if (p_core == null || p_equipmentCore == null || p_resourceLoader == null)
            {
                Debug.LogError("CombatFlow의 외부 참조가 설정되지 않았습니다.");

                return;
            }

            // 재연결 시 이전 Equipment 이벤트를 제거한다.
            if (_equipmentCore != null)
            {
                _equipmentCore.OnEquippedWeaponChanged -= HandleEquippedWeaponChanged;
            }

            _core = p_core;
            _equipmentCore = p_equipmentCore;
            _resourceLoader = p_resourceLoader;

            _equipmentCore.OnEquippedWeaponChanged += HandleEquippedWeaponChanged;

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

        private void OnDestroy()
        {
            if (_equipmentCore == null)
                return;

            _equipmentCore.OnEquippedWeaponChanged -= HandleEquippedWeaponChanged;
        }

        private void Update()
        {
            if (!IsBound) return;

            TickFlow();
            UpdateAimState();
        }

        // 장비
        // 장비 변경 이벤트 판단
        private void HandleEquippedWeaponChanged(EWeaponType p_type, WeaponDTO p_weapon)
        {
            // 현재 사용 중인 무기 종류와 관계없는 슬롯 변경은 무시한다.
            if (_core.EquipmentContext.CurrentWeaponType != p_type)
                return;

            if (p_weapon == null)
            {
                if (!_core.CombatModule.TryClearWeapon())
                    return;

                // 무기 장착 해제시
                _core.EquipmentView?.TryClearWeapon();
                _core.AnimationView?.ApplyWeaponOverrideController(EWeaponType.None);


                return;
            }

            // 같은 장비 슬롯의 아이템이 교체되었다면 현재 무기도 갱신한다.
            TryApplyWeapon(p_weapon);
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
            if (!IsBound || _core.BlockCombat || CurrentState?.Type != ECombatStateType.Idle || !CanWeaponSwap())
            {
                return false;
            }

            _core.CombatContext.ClearPendingWeapon();

            // 입력의 0, 1, 2를 장비 무기 종류로 변환한다.
            if (p_slotIndex < (int)EWeaponType.Melee ||
                p_slotIndex > (int)EWeaponType.Special)
            {
                return false;
            }

            EWeaponType weaponType = (EWeaponType)p_slotIndex;

            // 비어 있는 장비 슬롯은 Swap 대상으로 선택하지 않는다.
            if (!_equipmentCore.TryGetEquippedWeapon(weaponType, out WeaponDTO weapon))
            {
                return false;
            }

            // 현재 사용 중인 동일한 무기는 다시 선택하지 않는다.
            WeaponDTO currentWeapon = _core.EquipmentContext.CurrentWeapon;

            if (currentWeapon != null && currentWeapon.Id == weapon.Id)
            {
                return false;
            }

            _core.CombatContext.PendingWeaponType = weaponType;

            return true;
        }

        // 2. 미리 저장된 무기 교체 요청을 실제 무기 변경으로 확정
        // WeaponSwapState.Enter()에서 호출
        // 요청한 무기가 여전히 장착돼 있는지 재검증
        // TryApplyWeapon()을 통해 실제 상태와 프리팹 변경
        internal bool TryExecutePendingWeaponSwap()
        {
            if (!IsBound || !_core.CombatContext.HasPendingWeapon)
            {
                return false;
            }

            EWeaponType weaponType = _core.CombatContext.PendingWeaponType;

            // 실행 직전에 장비 상태를 다시 검증한다.
            if (!_equipmentCore.TryGetEquippedWeapon(weaponType, out WeaponDTO weapon))
            {
                return false;
            }

            return TryApplyWeapon(weapon);
        }

        // 3. 실제 상태와 프리팹 변경
        private bool TryApplyWeapon(WeaponDTO p_weapon)
        {
            if (p_weapon == null || _core.EquipmentView == null)
                return false;

            GameObject prefab = _resourceLoader.GetItemPrefab(p_weapon.ItemType, p_weapon.PrefabKey);

            if (prefab == null)
                return false;

            WeaponDTO previousWeapon = _core.EquipmentContext.CurrentWeapon;

            if (!_core.CombatModule.TrySwapWeapon(p_weapon))
                return false;

            if (_core.EquipmentView.TryShowWeapon(prefab))
                return true;

            // 외형 변경 실패 시 상태를 복구한다.
            if (previousWeapon == null)
                _core.CombatModule.TryClearWeapon();
            else
                _core.CombatModule.TrySwapWeapon(previousWeapon);

            return false;
        }
        private bool CanWeaponSwap()
        {
            if (_core.BlockCombat)
                return false;

            return _core.LocomotionContext.CurrentState switch
            {
                ELocoStateType.Dash => false,
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
    }
}
