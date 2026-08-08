using System;
using System.Collections.Generic;
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
        private AnimatorOverrideController _meleeOverrideController;

        [SerializeField]
        private AnimatorOverrideController _rangeOverrideController;

        [SerializeField]
        private AnimatorOverrideController _specialOverrideController;

        [Header("Melee Combo Slots")]
        [SerializeField]
        private AnimationClip[] _meleeComboSlots;

        [Header("Melee Layer Blend")]
        [SerializeField, Min(0f)]
        private float _meleeLayerEnterDuration = 0.05f;

        [SerializeField, Min(0f)]
        private float _meleeLayerExitDuration = 0.15f;

        private AnimatorOverrideController _runtimeMeleeController;
        private List<KeyValuePair<AnimationClip, AnimationClip>> _meleeOverrides;

        private const int BaseLayer = 0;

        private int _meleeFullBodyLayerIndex = -1;
        private const string MeleeFullBodyLayerName = "Weapon FullBody Layer";

        private float _meleeLayerTargetWeight;
        private float _meleeLayerBlendSpeed;

        public event Action<Vector3> OnRootMotion;

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

        private int _currentBaseState;


        private RuntimeAnimatorController _initialController;

        private static readonly int[] MeleeComboStates =
        {
            Animator.StringToHash("Weapon FullBody Layer.Combo1"),
            Animator.StringToHash("Weapon FullBody Layer.Combo2"),
            Animator.StringToHash("Weapon FullBody Layer.Combo3")
        };

        private int _isSprint = Animator.StringToHash("IsSprint");
        private int _isIncombat = Animator.StringToHash("IsInCombat");
        private int _isGround = Animator.StringToHash("IsGround");


        private readonly int _swap = Animator.StringToHash("Swap");
        private const string WeaponUpperBodyLayerName = "Weapon UpperBody Layer";
        private int _weaponUpperBodyLayerIndex = -1;

        private Transform _playerTr;

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

            InitializeMeleeController();
        }

        private void Update()
        {
            UpdateMeleeLayerBlend();
        }

        // Animator 평가가 끝난 프레임 이동량을 실제 이동 Module에 전달한다.
        private void OnAnimatorMove()
        {
            if (_anim == null)
                return;

            OnRootMotion?.Invoke(_anim.deltaPosition);
        }

        // Player 전용으로 생성한 런타임 Controller를 함께 정리한다.
        private void OnDestroy()
        {
            if (_runtimeMeleeController != null)
                Destroy(_runtimeMeleeController);
        }

        // 애니메이션 기준으로 사용할 Player Transform을 연결한다.
        public void Bind(Transform p_playerTr)
        {
            _playerTr = p_playerTr;
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
            Vector2 input = Vector2.ClampMagnitude(p_moveInput, 1f);

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

        // 점프 애니메이션으로 전환한다.
        public void PlayJump()
        {
            CrossFadeBase(Jump, 0.15f, 0);
        }
        // 낙하 애니메이션으로 전환한다.
        public void PlayFall()
        {
            CrossFadeBase(Fall, 0.15f, 0f);
        }
        // 착지 클립의 충격 구간부터 재생한다.
        public void PlayLand()
        {
            CrossFadeBase(Land, 0.143f, 0.443f);
        }

        // 연속 입력도 재생되도록 Dash 애니메이션을 강제로 처음부터 실행한다.
        public void PlayDash()
        {
            // 연속 Dash 입력에도 처음부터 재생한다.
            CrossFadeBase(DashState, 0.05f, 0f, true);
        }


        #endregion ======================================== /Locomotion

        #region ============================== WeaponOvrrideController
        // 무기 종류에 맞는 AnimatorOverrideController를 적용하고 상체 Layer를 복구한다.
        public void ApplyWeaponOverrideController(EWeaponType p_weaponType)
        {
            if (_anim == null)
                return;

            RuntimeAnimatorController nextController = GetWeaponOverrideController(p_weaponType);

            if (nextController == null)
            {
                Debug.LogError($"등록된 AnimatorOverrideController가 없습니다: {p_weaponType}", this);

                return;
            }

            if (_anim.runtimeAnimatorController == nextController)
                return;

            _anim.runtimeAnimatorController = nextController;
            RefreshWeaponLayerIndices();
        }

        // 공용 Melee Controller를 복사해 Player 전용 런타임 Controller를 준비한다.
        private void InitializeMeleeController()
        {
            if (_meleeOverrideController == null)
                return;

            _runtimeMeleeController =
                new AnimatorOverrideController(
                    _meleeOverrideController.runtimeAnimatorController);

            _meleeOverrides =
                new List<KeyValuePair<AnimationClip, AnimationClip>>(
                    _meleeOverrideController.overridesCount);
        }

        // 현재 Melee Prefab의 콤보 클립만 Player 전용 Controller에 적용한다.
        public bool ApplyMeleeWeapon(
            IReadOnlyList<AnimationClip> p_comboClips)
        {
            if (_anim == null ||
                _runtimeMeleeController == null ||
                _meleeOverrides == null ||
                _meleeComboSlots == null ||
                p_comboClips == null)
            {
                return false;
            }

            // 이전 무기의 변경값이 남지 않도록 공용 템플릿부터 다시 복사한다.
            _meleeOverrideController.GetOverrides(_meleeOverrides);

            int applyCount = Mathf.Min(
                _meleeComboSlots.Length,
                p_comboClips.Count);

            for (int index = 0; index < applyCount; index++)
            {
                AnimationClip slotClip = _meleeComboSlots[index];
                AnimationClip weaponClip = p_comboClips[index];

                if (slotClip == null || weaponClip == null)
                    continue;

                if (!ReplaceOverride(
                    _meleeOverrides,
                    slotClip,
                    weaponClip))
                {
                    Debug.LogError(
                        $"Melee Combo 원본 슬롯을 찾을 수 없습니다: {slotClip.name}",
                        this);
                    return false;
                }
            }

            // 여러 콤보 슬롯을 한 번에 변경해 Clip Binding 갱신을 한 번만 수행한다.
            _runtimeMeleeController.ApplyOverrides(_meleeOverrides);
            _anim.runtimeAnimatorController = _runtimeMeleeController;

            RefreshWeaponLayerIndices();
            return true;
        }

        // 원본 슬롯을 찾아 현재 무기 Prefab의 클립으로 교체한다.
        private static bool ReplaceOverride(
            List<KeyValuePair<AnimationClip, AnimationClip>> p_overrides,
            AnimationClip p_slotClip,
            AnimationClip p_weaponClip)
        {
            for (int index = 0; index < p_overrides.Count; index++)
            {
                if (p_overrides[index].Key != p_slotClip)
                    continue;

                p_overrides[index] =
                    new KeyValuePair<AnimationClip, AnimationClip>(
                        p_slotClip,
                        p_weaponClip);

                return true;
            }

            return false;
        }

        // Controller 변경 후 Layer Index와 기본 Weight를 다시 설정한다.
        private void RefreshWeaponLayerIndices()
        {
            _weaponUpperBodyLayerIndex =
                _anim.GetLayerIndex(WeaponUpperBodyLayerName);

            _meleeFullBodyLayerIndex =
                _anim.GetLayerIndex(MeleeFullBodyLayerName);

            if (_weaponUpperBodyLayerIndex >= 0)
                _anim.SetLayerWeight(_weaponUpperBodyLayerIndex, 1f);

            if (_meleeFullBodyLayerIndex >= 0)
                SetMeleeLayerWeightImmediate(0f);
        }

        // 무기 종류에 대응하는 Override Controller를 반환한다.
        private RuntimeAnimatorController GetWeaponOverrideController(EWeaponType p_weaponType)
        {
            // 장비가 없으면 전용 비무장 Controller 또는 최초 Controller를 사용한다.
            switch (p_weaponType)
            {
                case EWeaponType.Melee:
                    return _meleeOverrideController;

                case EWeaponType.Range:
                    return _rangeOverrideController;

                case EWeaponType.Special:
                    return _specialOverrideController;

                case EWeaponType.None:
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
            if (_anim == null) return;

            if (_weaponUpperBodyLayerIndex >= 0)
            {
                _anim.SetLayerWeight(_weaponUpperBodyLayerIndex, 1f);
            }

            _anim.ResetTrigger(_swap);
            _anim.SetTrigger(_swap);
        }

        // 지정한 콤보 순서의 전신 공격 애니메이션을 재생한다.
        public void PlayMeleeCombo(int p_comboIndex)
        {
            if (_anim == null || _meleeFullBodyLayerIndex < 0 ||
                p_comboIndex < 0 || p_comboIndex >= MeleeComboStates.Length)
            {
                return;
            }

            BlendMeleeLayerWeight(1f, _meleeLayerEnterDuration);

            _anim.CrossFadeInFixedTime(MeleeComboStates[p_comboIndex], 0.05f, _meleeFullBodyLayerIndex);
        }

        // 근접 공격 표현을 끝내고 이동 애니메이션을 다시 노출한다.
        public void StopMeleeAction()
        {
            if (_anim == null || _meleeFullBodyLayerIndex < 0)
                return;

            BlendMeleeLayerWeight(0f, _meleeLayerExitDuration);
        }

        private void BlendMeleeLayerWeight(
            float p_targetWeight,
            float p_duration)
        {
            float currentWeight =
                _anim.GetLayerWeight(_meleeFullBodyLayerIndex);

            _meleeLayerTargetWeight = Mathf.Clamp01(p_targetWeight);

            if (p_duration <= 0f ||
                Mathf.Approximately(currentWeight, _meleeLayerTargetWeight))
            {
                SetMeleeLayerWeightImmediate(_meleeLayerTargetWeight);
                return;
            }

            _meleeLayerBlendSpeed =
                Mathf.Abs(_meleeLayerTargetWeight - currentWeight) /
                p_duration;
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

            _anim.SetLayerWeight(
                _meleeFullBodyLayerIndex,
                nextWeight);

            if (Mathf.Approximately(
                    nextWeight,
                    _meleeLayerTargetWeight))
            {
                _meleeLayerBlendSpeed = 0f;
            }
        }

        private void SetMeleeLayerWeightImmediate(float p_weight)
        {
            _meleeLayerTargetWeight = Mathf.Clamp01(p_weight);
            _meleeLayerBlendSpeed = 0f;

            _anim.SetLayerWeight(
                _meleeFullBodyLayerIndex,
                _meleeLayerTargetWeight);
        }
        #endregion ============================== /Combat
    }
}
