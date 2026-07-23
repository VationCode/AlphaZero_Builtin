using UnityEngine;
using UnityEngine.Windows;
using static UnityEditor.Experimental.GraphView.GraphView;

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

    private static readonly int Dash =
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

    public void Bind(Transform p_playerTr)
    {
        _playerTr = p_playerTr;
    }
    // p_transitionDuration 전환 비율
    // p_normalizedTimeOffset 클립의 0%부터 재생
    private void CrossFadeBase(int p_stateHash, float p_transitionDuration = 0.15f, 
                               float p_normalizedTimeOffset = 0f, bool p_forceReplay = false)
    {
        // 같은 애니메이션을 매 프레임 재실행하지 않음
        if (!p_forceReplay && _currentBaseState == p_stateHash)
            return;

        _currentBaseState = p_stateHash;

        // 초 단위 전환 시간이 관리하기 편함
        _anim.CrossFadeInFixedTime(p_stateHash, 0.15f, BaseLayer, p_normalizedTimeOffset);
    }

    #region ======================================== Locomotion
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
            bool isMoving = input.sqrMagnitude > 0.01f;

            targetState = p_isSprint && isMoving ? SprintState : MovementState;

            _anim.SetFloat(MoveMagnitude, input.magnitude, 0.1f, Time.deltaTime);
        }

        CrossFadeBase(targetState);
    }

    public void PlayJump()
    {
        CrossFadeBase(Jump);
    }

    public void PlayDash()
    {
        CrossFadeBase(Dash);
    }

    public void PlayFall()
    {
        CrossFadeBase(Fall, 0, 0.15f); 
    }

    public void PlayLand()
    {
        CrossFadeBase(Land, 0.143f, 0.443f);
    }
    #endregion ======================================== /Locomotion




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

        // 모든 Override Controller가 같은 Base Controller를 사용하지만
        // 안전하게 Layer Index와 Weight를 다시 확인한다.
        _weaponUpperBodyLayerIndex = _anim.GetLayerIndex(WeaponUpperBodyLayerName);

        if (_weaponUpperBodyLayerIndex >= 0)
        {
            _anim.SetLayerWeight(_weaponUpperBodyLayerIndex, 1f);
        }
    }
    private RuntimeAnimatorController GetWeaponOverrideController(EWeaponType p_weaponType)
    {
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

    

    

    public void IsGround(bool p_isGround)
    {
        _anim.SetBool(_isGround, p_isGround);
    }

    public void FlyUp()
    {

    }

    public void IsFlyUpDownPos(bool p_isFly)
    {
        //_flyUpDownAnim.SetBool("IsFly", p_isFly);
    }

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
