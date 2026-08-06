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

        private const int BaseLayer = 0;
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
            _initialController = _anim.runtimeAnimatorController;

            _weaponUpperBodyLayerIndex = _anim.GetLayerIndex(WeaponUpperBodyLayerName);

            if (_weaponUpperBodyLayerIndex < 0)
            {
                Debug.LogError($"Animator Layer를 찾을 수 없습니다: {WeaponUpperBodyLayerName}", this);
                return;
            }

            _anim.SetLayerWeight(_weaponUpperBodyLayerIndex, 1f);
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

            // Controller 교체 후 달라질 수 있는 Layer Index와 Weight를 다시 확인한다.
            _weaponUpperBodyLayerIndex = _anim.GetLayerIndex(WeaponUpperBodyLayerName);

            if (_weaponUpperBodyLayerIndex >= 0)
            {
                _anim.SetLayerWeight(_weaponUpperBodyLayerIndex, 1f);
            }
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
    }
}
