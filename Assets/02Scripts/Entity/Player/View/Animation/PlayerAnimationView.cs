using System;
using Alpha.Combat;
using Alpha.Player.Actions;
using Alpha.Player.Combat;
using Alpha.Player.Locomotion;
using UnityEngine;

namespace Alpha.Player.Animation
{
    // Player의 이동·전투 상태를 Animator 파라미터와 Override Controller로 표현한다.
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimationView : MonoBehaviour
    {
        private Animator _anim;

        [Header("Weapon Animator Override")]
        [SerializeField]
        private AnimatorOverrideController _unarmedOverrideController;

        [SerializeField]
        private AnimatorOverrideController _rangeOverrideController;

        [SerializeField]
        private AnimatorOverrideController _specialOverrideController;

        [Header("Melee Layer Blend")]
        [SerializeField, Min(0f)]
        private float _meleeLayerEnterDuration = 0.05f;

        [SerializeField, Min(0f)]
        private float _meleeLayerExitDuration = 0.15f;

        private const int BaseLayer = 0;

        private int _meleeFullBodyLayerIndex = -1;
        private const string MeleeFullBodyLayerName = "Weapon FullBody Layer";

        private float _meleeLayerTargetWeight;
        private float _meleeLayerBlendSpeed;

        public event Action<Vector3> OnRootMotion;
        public event Action OnFootstep;

        [Header("Footstep")]
        [SerializeField, Range(0f, 1f)]
        private float _firstFootstepPhase = 0.2f;

        [SerializeField, Range(0f, 1f)]
        private float _secondFootstepPhase = 0.7f;

        private bool _hasGroundMoveInput;
        private bool _isFootstepCycleActive;
        private int _footstepStateHash;
        private float _previousFootstepPhase;

        private static readonly int MovementState =
            Animator.StringToHash("Base Layer.MovementTree");

        private static readonly int CombatMovementState =
        Animator.StringToHash("Base Layer.CombatMovementTree");

        private static readonly int SprintState =
            Animator.StringToHash("Base Layer.Fast Run F");

        private static readonly int MoveMagnitude =
            Animator.StringToHash("MoveMagnitude");

        private static readonly int InputX =
            Animator.StringToHash("InputX");

        private static readonly int InputY =
            Animator.StringToHash("InputY");

        private static readonly int Jump =
            Animator.StringToHash("Base Layer.Jump");

        private static readonly int Fall =
            Animator.StringToHash("Base Layer.Fall");

        private static readonly int Land =
            Animator.StringToHash("Base Layer.Land");

        private static readonly int DashState =
            Animator.StringToHash("Base Layer.Dash");

        private static readonly int DodgeState =
            Animator.StringToHash("Base Layer.Dodge Tree");

        private static readonly int LightHitReactionState =
            Animator.StringToHash("Base Layer.LightHit");

        private static readonly int HeavyHitReactionState =
            Animator.StringToHash("Base Layer.HeavyHit");

        private static readonly int KnockdownState =
            Animator.StringToHash("Base Layer.KnockDown");

        private static readonly int LyingDownState =
            Animator.StringToHash("Base Layer.LyingDown");

        private static readonly int StandUpState =
            Animator.StringToHash("Base Layer.StandUp");

        private int _currentBaseState;
        private bool _isDamageReactionActive;
        private PlayerActionFlow _actionFlow;
        private LocomotionContext _locomotionContext;
        private CombatContext _combatContext;
        private bool _isActionSubscribed;


        private RuntimeAnimatorController _initialController;

        private static readonly int MeleeGuardState =
            Animator.StringToHash("Weapon FullBody Layer.Guard");

        private int _isSprint = Animator.StringToHash("IsSprint");
        private int _isIncombat = Animator.StringToHash("IsInCombat");
        private int _isGround = Animator.StringToHash("IsGround");


        private readonly int _swap = Animator.StringToHash("Swap");
        private const string WeaponUpperBodyLayerName = "Weapon UpperBody Layer";
        private int _weaponUpperBodyLayerIndex = -1;

        // Animator와 기본 Controller를 보관하고 무기 상체 Layer를 초기화한다.
        private void Awake()
        {
            _anim = GetComponent<Animator>();

            // Root Motion은 OnAnimatorMove에서 CharacterController 경로로 직접 적용한다.
            _anim.applyRootMotion = true;

            // Melee FullBody 추가 Layer의 이동도 deltaPosition 계산에 포함한다.
            _anim.layersAffectMassCenter = true;

            _initialController = _anim.runtimeAnimatorController;

            _weaponUpperBodyLayerIndex = _anim.GetLayerIndex(WeaponUpperBodyLayerName);

            if (_weaponUpperBodyLayerIndex < 0)
            {
                Debug.LogError($"Animator Layer를 찾을 수 없습니다: {WeaponUpperBodyLayerName}", this);
                return;
            }

            _anim.SetLayerWeight(_weaponUpperBodyLayerIndex, 1f);

            _meleeFullBodyLayerIndex = _anim.GetLayerIndex(MeleeFullBodyLayerName);
            if (_meleeFullBodyLayerIndex < 0)
            {
                Debug.LogError($"Animator Layer를 찾을 수 없습니다: {MeleeFullBodyLayerName}", this);
            }
            else
            {
                SetMeleeLayerWeightImmediate(0f);
            }

        }

        // Action 이벤트와 현재 이동 상태를 Player Animator 표현에 연결한다.
        public void Bind(
            PlayerActionFlow p_actionFlow,
            LocomotionContext p_locomotionContext,
            CombatContext p_combatContext)
        {
            Unbind();
            _actionFlow = p_actionFlow;
            _locomotionContext = p_locomotionContext;
            _combatContext = p_combatContext;
            SubscribeToAction();
        }

        public void Unbind()
        {
            UnsubscribeFromAction();
            _actionFlow = null;
            _locomotionContext = null;
            _combatContext = null;
        }

        private void SubscribeToAction()
        {
            if (_isActionSubscribed ||
                !isActiveAndEnabled ||
                _actionFlow == null)
            {
                return;
            }

            _actionFlow.OnHitReactionStateChanged += HandleHitReactionStateChanged;
            _actionFlow.OnDeathStarted += HandleDeathStarted;
            _actionFlow.OnDeathDownStarted += HandleDeathDownStarted;
            _isActionSubscribed = true;

            if (_actionFlow.IsDead)
            {
                if (_actionFlow.IsDeathFalling)
                    PlayKnockdown();
                else
                    PlayLyingDown();
            }
            else
            {
                HandleHitReactionStateChanged(_actionFlow.HitReactionState);
            }
        }

        private void UnsubscribeFromAction()
        {
            if (!_isActionSubscribed)
                return;

            if (_actionFlow != null)
            {
                _actionFlow.OnHitReactionStateChanged -= HandleHitReactionStateChanged;
                _actionFlow.OnDeathStarted -= HandleDeathStarted;
                _actionFlow.OnDeathDownStarted -= HandleDeathDownStarted;
            }

            _isActionSubscribed = false;
        }

        private void HandleHitReactionStateChanged(EHitReactionState p_state)
        {
            if (p_state == EHitReactionState.None)
            {
                // View가 비활성화된 동안 반응이 끝난 경우에도 표현 잠금을 복구한다.
                EndDamageReaction();
                RestoreLocomotionPresentation();
                return;
            }

            PlayHitReaction(p_state);
        }

        private void HandleDeathStarted()
        {
            PlayKnockdown();
        }

        private void HandleDeathDownStarted()
        {
            PlayLyingDown();
        }

        // 피격 종료 시 현재 Locomotion 상태의 표현으로 되돌린다.
        private void RestoreLocomotionPresentation()
        {
            if (_locomotionContext == null)
                return;

            switch (_locomotionContext.CurrentState)
            {
                case ELocoStateType.Jump:
                    PlayJump();
                    break;

                case ELocoStateType.Fall:
                    PlayFall();
                    break;

                case ELocoStateType.Land:
                    PlayLand();
                    break;

                case ELocoStateType.Dash:
                    PlayDash();
                    break;

                case ELocoStateType.Dodge:
                    // DodgeState가 보관한 입력 방향으로 직접 재생한다.
                    break;

                default:
                    PlayGroundLocomotion(
                        Vector2.zero,
                        false,
                        _combatContext?.IsCombatStanceActive == true);
                    break;
            }
        }

        private void Update()
        {
            UpdateMeleeLayerBlend();
        }

        private void OnEnable()
        {
            SubscribeToAction();
        }

        private void OnDisable()
        {
            UnsubscribeFromAction();
        }

        // Animator 평가가 끝난 뒤 이동 애니메이션의 발걸음 주기를 확인한다.
        private void LateUpdate()
        {
            UpdateFootstepCycle();
        }

        // Animator 평가가 끝난 프레임 이동량을 실제 이동 Module에 전달한다.
        private void OnAnimatorMove()
        {
            if (_anim == null)
                return;

            OnRootMotion?.Invoke(_anim.deltaPosition);
        }

        /// <summary>
        /// Base Layer의 상태를 전환하며, 강제 재생이 아니면 같은 상태의 중복 전환을 생략한다.
        /// </summary>
        /// <param name="p_stateHash"></param>
        /// <param name="p_transitionDuration"> 전환 비율 </param>
        /// <param name="p_normalizedTimeOffset"> 클립의 %부터 재생 </param>
        /// <param name="p_forceReplay"> 재실행 여부 </param>
        private void CrossFadeBase(int p_stateHash, float p_transitionDuration = 0.15f, 
                                   float p_normalizedTimeOffset = 0f, bool p_forceReplay = false)
        {
            // 같은 애니메이션을 매 프레임 재실행하지 않음
            if (!p_forceReplay && _currentBaseState == p_stateHash)
                return;

            _currentBaseState = p_stateHash;

            // 초 단위 전환 시간이 관리하기 편함
            _anim.CrossFadeInFixedTime(p_stateHash, p_transitionDuration, BaseLayer, p_normalizedTimeOffset);
        }

        #region ======================================== Locomotion
        // 일반 이동과 전투 이동에 맞는 BlendTree를 선택하고 입력 파라미터를 갱신한다.
        public void PlayGroundLocomotion(Vector2 p_moveInput, bool p_isSprint, bool p_isCombat = false)
        {
            if (_isDamageReactionActive)
                return;

            Vector2 input = Vector2.ClampMagnitude(p_moveInput, 1f);
            _hasGroundMoveInput = input.sqrMagnitude > 0.01f;

            int targetState;

            if (p_isCombat)
            {
                targetState = CombatMovementState;

                // 전투 이동 방향 BlendTree 갱신
                _anim.SetFloat(InputX, input.x, 0.1f, Time.deltaTime);
                _anim.SetFloat(InputY, input.y, 0.1f, Time.deltaTime);
            }
            else
            {
                // 이동 중 Sprint일 때만 전용 상태를 사용한다.
                bool isMoving = input.sqrMagnitude > 0.01f;

                targetState = p_isSprint && isMoving ? SprintState : MovementState;

                _anim.SetFloat(MoveMagnitude, input.magnitude, 0.1f, Time.deltaTime);
            }

            CrossFadeBase(targetState);
        }

        // 현재 이동 BlendTree의 한 주기에서 양발 접촉 시점을 알린다.
        private void UpdateFootstepCycle()
        {
            if (!_hasGroundMoveInput)
            {
                ResetFootstepCycle();
                return;
            }

            AnimatorStateInfo stateInfo =
                _anim.GetCurrentAnimatorStateInfo(BaseLayer);

            int stateHash = stateInfo.fullPathHash;

            if (stateHash != MovementState &&
                stateHash != CombatMovementState &&
                stateHash != SprintState)
            {
                ResetFootstepCycle();
                return;
            }

            float currentPhase = Mathf.Repeat(
                stateInfo.normalizedTime,
                1f);

            if (!_isFootstepCycleActive ||
                _footstepStateHash != stateHash)
            {
                _isFootstepCycleActive = true;
                _footstepStateHash = stateHash;
                _previousFootstepPhase = currentPhase;
                return;
            }

            if (HasCrossedFootstepPhase(
                    _previousFootstepPhase,
                    currentPhase,
                    _firstFootstepPhase))
            {
                OnFootstep?.Invoke();
            }

            if (HasCrossedFootstepPhase(
                    _previousFootstepPhase,
                    currentPhase,
                    _secondFootstepPhase))
            {
                OnFootstep?.Invoke();
            }

            _previousFootstepPhase = currentPhase;
        }

        private static bool HasCrossedFootstepPhase(
            float p_previous,
            float p_current,
            float p_target)
        {
            return p_current >= p_previous
                ? p_target > p_previous && p_target <= p_current
                : p_target > p_previous || p_target <= p_current;
        }

        private void ResetFootstepCycle()
        {
            _isFootstepCycleActive = false;
            _footstepStateHash = 0;
            _previousFootstepPhase = 0f;
        }

        // 점프 애니메이션으로 전환한다.
        public void PlayJump()
        {
            if (_isDamageReactionActive)
                return;

            CrossFadeBase(Jump, 0.15f, 0);
        }
        // 낙하 애니메이션으로 전환한다.
        public void PlayFall()
        {
            if (_isDamageReactionActive)
                return;

            CrossFadeBase(Fall, 0.15f, 0f);
        }
        // 착지 클립의 충격 구간부터 재생한다.
        public void PlayLand()
        {
            if (_isDamageReactionActive)
                return;

            CrossFadeBase(Land, 0.143f, 0.443f);
        }

        // 연속 입력도 재생되도록 Dash 애니메이션을 강제로 처음부터 실행한다.
        public void PlayDash()
        {
            if (_isDamageReactionActive)
                return;

            // 연속 Dash 입력에도 처음부터 재생한다.
            CrossFadeBase(DashState, 0.05f, 0f, true);
        }

        // Player 로컬 방향을 8방향 Dodge Blend Tree에 전달하고 처음부터 재생한다.
        public void PlayDodge(Vector2 p_localDirection)
        {
            if (_isDamageReactionActive)
                return;

            Vector2 direction = Vector2.ClampMagnitude(
                p_localDirection,
                1f);

            if (direction.sqrMagnitude <= 0.0001f)
                return;

            direction.Normalize();
            _hasGroundMoveInput = false;
            ResetFootstepCycle();

            // 취소된 Melee 전신 동작이 Dodge를 덮지 않도록 즉시 내린다.
            if (_meleeFullBodyLayerIndex >= 0)
                SetMeleeLayerWeightImmediate(0f);

            _anim.SetFloat(InputX, direction.x);
            _anim.SetFloat(InputY, direction.y);
            CrossFadeBase(DodgeState, 0.05f, 0f, true);
        }

        // 공용 피격 행동 상태를 Player Base Layer 상태로 변환한다.
        public bool PlayHitReaction(EHitReactionState p_state)
        {
            switch (p_state)
            {
                case EHitReactionState.LightHit:
                    PlayDamageState(LightHitReactionState);
                    return true;

                case EHitReactionState.HeavyHit:
                    PlayDamageState(HeavyHitReactionState);
                    return true;

                case EHitReactionState.Knockdown:
                    PlayKnockdown();
                    return true;

                case EHitReactionState.LyingDown:
                    PlayLyingDown();
                    return true;

                case EHitReactionState.StandUp:
                    PlayStandUp();
                    return true;

                default:
                    return false;
            }
        }

        public void PlayKnockdown()
        {
            PlayDamageState(KnockdownState);
        }

        public void PlayLyingDown()
        {
            PlayDamageState(LyingDownState);
        }

        public void PlayStandUp()
        {
            PlayDamageState(StandUpState);
        }

        // 피격 전용 표현 잠금을 해제하고 다음 Locomotion 요청을 다시 받는다.
        public void EndDamageReaction()
        {
            if (!_isDamageReactionActive)
                return;

            _isDamageReactionActive = false;
            _currentBaseState = 0;

            if (_weaponUpperBodyLayerIndex >= 0)
                _anim.SetLayerWeight(_weaponUpperBodyLayerIndex, 1f);

            if (_meleeFullBodyLayerIndex >= 0)
                SetMeleeLayerWeightImmediate(0f);
        }

        private void PlayDamageState(int p_stateHash)
        {
            _isDamageReactionActive = true;
            _hasGroundMoveInput = false;
            ResetFootstepCycle();

            // 상체·근접 Layer가 Base Layer의 피격 자세를 덮지 않게 한다.
            if (_weaponUpperBodyLayerIndex >= 0)
                _anim.SetLayerWeight(_weaponUpperBodyLayerIndex, 0f);

            if (_meleeFullBodyLayerIndex >= 0)
                SetMeleeLayerWeightImmediate(0f);

            CrossFadeBase(
                p_stateHash,
                0.05f,
                0f,
                true);
        }


        #endregion ======================================== /Locomotion

        #region ============================== WeaponOvrrideController
        // 무기 종류에 맞는 AnimatorOverrideController를 적용하고 상체 Layer를 복구한다.
        public void ApplyWeaponOverrideController(
            EWeaponCategory p_weaponCategory)
        {
            if (_anim == null)
                return;

            RuntimeAnimatorController nextController =
                GetWeaponOverrideController(p_weaponCategory);

            if (nextController == null)
            {
                Debug.LogError(
                    $"등록된 AnimatorOverrideController가 없습니다: {p_weaponCategory}",
                    this);

                return;
            }

            if (_anim.runtimeAnimatorController == nextController)
                return;

            _anim.runtimeAnimatorController = nextController;
            RefreshWeaponLayerIndices();
        }

        // 무기가 소유한 Override Controller를 적용해 상태 구조는 유지하고 모션만 변경한다.
        public bool ApplyMeleeWeapon(
            AnimatorOverrideController p_overrideController)
        {
            if (_anim == null || p_overrideController == null)
                return false;

            RuntimeAnimatorController expectedBaseController =
                GetBaseController(_initialController);
            RuntimeAnimatorController overrideBaseController =
                GetBaseController(p_overrideController);

            if (expectedBaseController != null &&
                overrideBaseController != expectedBaseController)
            {
                Debug.LogError(
                    "Melee AnimatorOverrideController의 원본 Controller가 Player Animator와 다릅니다.",
                    this);
                return false;
            }

            _anim.runtimeAnimatorController = p_overrideController;
            RefreshWeaponLayerIndices();
            return true;
        }

        private static RuntimeAnimatorController GetBaseController(
            RuntimeAnimatorController p_controller)
        {
            RuntimeAnimatorController currentController = p_controller;

            while (currentController is AnimatorOverrideController overrideController)
            {
                currentController =
                    overrideController.runtimeAnimatorController;
            }

            return currentController;
        }

        // Controller 변경 후 Layer Index와 기본 Weight를 다시 설정한다.
        private void RefreshWeaponLayerIndices()
        {
            _weaponUpperBodyLayerIndex =
                _anim.GetLayerIndex(WeaponUpperBodyLayerName);

            _meleeFullBodyLayerIndex =
                _anim.GetLayerIndex(MeleeFullBodyLayerName);

            if (_weaponUpperBodyLayerIndex >= 0)
            {
                _anim.SetLayerWeight(
                    _weaponUpperBodyLayerIndex,
                    _isDamageReactionActive ? 0f : 1f);
            }

            if (_meleeFullBodyLayerIndex >= 0)
                SetMeleeLayerWeightImmediate(0f);
        }

        // 무기 종류에 대응하는 Override Controller를 반환한다.
        private RuntimeAnimatorController GetWeaponOverrideController(
            EWeaponCategory p_weaponCategory)
        {
            // 장비가 없으면 전용 비무장 Controller 또는 최초 Controller를 사용한다.
            switch (p_weaponCategory)
            {
                case EWeaponCategory.Melee:
                    return null;

                case EWeaponCategory.Range:
                    return _rangeOverrideController;

                case EWeaponCategory.Special:
                    return _specialOverrideController;

                case EWeaponCategory.None:
                    return _unarmedOverrideController != null
                        ? _unarmedOverrideController
                        : _initialController;

                default:
                    return null;
            }
        }

        #endregion

        // 지상 여부를 Animator 파라미터에 반영한다.
        public void IsGround(bool p_isGround)
        {
            _anim.SetBool(_isGround, p_isGround);
        }

        // 비행 상승 애니메이션 확장을 위한 진입점이다.
        public void FlyUp()
        {

        }

        // 비행 상승·하강 상태 반영을 위한 확장 지점이다.
        public void IsFlyUpDownPos(bool p_isFly)
        {
            //_flyUpDownAnim.SetBool("IsFly", p_isFly);
        }

        #region ============================== Combat
        // 상체 Layer를 활성화하고 무기 교체 Trigger를 다시 발생시킨다.
        public void PlayWeaponSwap()
        {
            if (_anim == null || _isDamageReactionActive)
                return;

            if (_weaponUpperBodyLayerIndex >= 0)
            {
                _anim.SetLayerWeight(_weaponUpperBodyLayerIndex, 1f);
            }

            _anim.ResetTrigger(_swap);
            _anim.SetTrigger(_swap);
        }

        // Skill Animation Key와 같은 전신 Layer의 Animator 상태를 직접 재생한다.
        public bool PlayMeleeSkill(string p_stateName)
        {
            if (_anim == null || _isDamageReactionActive ||
                _meleeFullBodyLayerIndex < 0 ||
                string.IsNullOrWhiteSpace(p_stateName))
            {
                return false;
            }

            string stateName = p_stateName.Trim();
            int stateHash = Animator.StringToHash(
                $"{MeleeFullBodyLayerName}.{stateName}");

            if (!_anim.HasState(_meleeFullBodyLayerIndex, stateHash))
            {
                Debug.LogWarning($"Melee Animator 상태를 찾을 수 없습니다: {stateName}", this);
                return false;
            }

            BlendMeleeLayerWeight(1f, _meleeLayerEnterDuration);

            _anim.CrossFadeInFixedTime(stateHash, 0.05f, _meleeFullBodyLayerIndex);
            return true;
        }

        // 우클릭을 유지하는 동안 반복할 근접 가드 애니메이션을 재생한다.
        public void PlayMeleeGuard()
        {
            if (_anim == null || _isDamageReactionActive ||
                _meleeFullBodyLayerIndex < 0)
                return;

            BlendMeleeLayerWeight(1f, _meleeLayerEnterDuration);
            _anim.CrossFadeInFixedTime(MeleeGuardState, 0.05f, _meleeFullBodyLayerIndex);
        }

        // 근접 공격 표현을 끝내고 이동 애니메이션을 다시 노출한다.
        public void StopMeleeAction()
        {
            if (_anim == null || _meleeFullBodyLayerIndex < 0)
                return;

            BlendMeleeLayerWeight(0f, _meleeLayerExitDuration);
        }

        private void BlendMeleeLayerWeight(float p_targetWeight, float p_duration)
        {
            float currentWeight =
                _anim.GetLayerWeight(_meleeFullBodyLayerIndex);

            _meleeLayerTargetWeight = Mathf.Clamp01(p_targetWeight);

            if (p_duration <= 0f || Mathf.Approximately(currentWeight, _meleeLayerTargetWeight))
            {
                SetMeleeLayerWeightImmediate(_meleeLayerTargetWeight);
                return;
            }

            _meleeLayerBlendSpeed =
                Mathf.Abs(_meleeLayerTargetWeight - currentWeight) / p_duration;
        }

        private void UpdateMeleeLayerBlend()
        {
            if (_anim == null ||
                _meleeFullBodyLayerIndex < 0 ||
                _meleeLayerBlendSpeed <= 0f)
            {
                return;
            }

            float currentWeight =
                _anim.GetLayerWeight(_meleeFullBodyLayerIndex);

            float nextWeight = Mathf.MoveTowards(
                currentWeight,
                _meleeLayerTargetWeight,
                _meleeLayerBlendSpeed * Time.deltaTime);

            _anim.SetLayerWeight(_meleeFullBodyLayerIndex, nextWeight);

            if (Mathf.Approximately(nextWeight, _meleeLayerTargetWeight))
            {
                _meleeLayerBlendSpeed = 0f;
            }
        }

        private void SetMeleeLayerWeightImmediate(float p_weight)
        {
            _meleeLayerTargetWeight = Mathf.Clamp01(p_weight);
            _meleeLayerBlendSpeed = 0f;

            _anim.SetLayerWeight(_meleeFullBodyLayerIndex, _meleeLayerTargetWeight);
        }
        #endregion ============================== /Combat
    }
}
