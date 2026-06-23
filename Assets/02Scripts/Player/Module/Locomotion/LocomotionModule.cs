using alpha.camera;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(CharacterController))]
public class LocomotionModule : MonoBehaviour
{
    // Ref
    [Header("[Ref]")]
    [SerializeField] private CharacterController _characterCtrl;
    private AnimationBoundary _anim;

    [Header("GroundMoveMove")]
    [SerializeField] private float _moveSpeed = 4f;
    [SerializeField] private float _sprintSpeed = 7f;
    [SerializeField] private float _combatSpeed = 2.5f;
    [SerializeField] private float _rotationsmoothTime = 0.1f;

    [Header("[Ground]")]
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _checkGroundOffset = 0.125f;

    [Header("[Gravity]")]
    [SerializeField] private float _gravity = 13f;

    [Header("[Action]")]
    [SerializeField] private float _jumpHeight = 8f;

    [SerializeField] private float _dashDistance = 7f;
    [SerializeField] private float _dashDuration = 0.4f;

    #region ========== RunTime

    [SerializeField] private bool _isSprint;
    private bool _isJump;
    [SerializeField] private bool _isJumping;
    private bool _isDash;
    public bool IsDahsing => _isDashing;
    [SerializeField] private bool _isDashing;
    [SerializeField] private bool _isCombatStart;
    [SerializeField] private bool _isAim;

    private Vector3 _inputMoveDir;

    private float _rotationSmoothVelocity;
    private float _currentSpeed;
    public Vector3 CurrentVelocity => _currentVelocity;
    private Vector3 _currentVelocity;
    private float _currentVelocityY;
    private float m_currentDashDistance;
    public bool IsGround => _isGround;
    private bool _isGround;
    private bool _enableGroundCheck = true;

    private Vector3 _dashDir;

    #endregion ========== /RunTime
    private void Awake()
    {
        _characterCtrl = GetComponent<CharacterController>();
    }

    public void Bind(PlayerCore p_core)
    {
        _anim = p_core.AnimationBoundary;
    }

    private void Start()
    {
        _currentSpeed = _moveSpeed;
    }

    private void Update()
    {
        /*_isSprint = _inputBoundary.IsSprint;
        _isJump = _inputBoundary.IsJump;
        _isDash = _inputBoundary.IsDash;
        _isAim = _inputBoundary.IsAim;
        _isCombat = _inputBoundary.IsAttack || _isAim;

        if (_isDash && !_isJumping && !_isDashing &&  !_isCombat)
        {
            _anim.DashAnim();
            StartDash();
        }

        if(!_isJumping && !_isDashing)
            ViewTransition(_isCombat, _isAim);

        CheckedGround();
        _anim.IsGround(_isGround);

       _currentVelocity = ApplyGravity();

        if (_isJump && !_isJumping && !_isDashing)
            Jump();
        else if (_isDashing)
            Dashing();
        else if (_isGround)
        {
            _currentVelocity = CalculateGroundMoveVelocity(_isSprint, _isCombat);
        }

        Rotate(_inputMoveDir, (_isJumping || _isDashing), _isCombat);
        _characterCtrl.Move(_currentVelocity * Time.deltaTime);*/

        Tick();
    }

    private void Tick()
    {
        _currentVelocity = ApplyGravity();

        CheckedGround();

        _anim.IsGround(_isGround);
    }

    /*private void ViewTransition(bool p_isCombat, bool p_isAim)
    {
        if (p_isCombat)
        {
            if (!_isCombatStart)
            {
                if (p_isAim)
                    _cameraModule.SetView(EViewType.ShoulderView);

                _isCombatStart = true;
            }
            _isSprint = false;
        }
        else
        {
            if (_isCombatStart)
            {
                _cameraModule.SetView(EViewType.BackView);
                _isCombatStart = false;
            }
        }
    }*/

    public void PlayCharacterController()
    {
        _characterCtrl.Move(_currentVelocity * Time.deltaTime);
    }

    public void GroundMoveMove(Vector3 p_moveInput, bool p_isSprint = false, bool p_isCombat = false)
    {
        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;

        camForward.y = 0f;
        camRight.y = 0f;

        // 바닥이나 하늘을 바라봤을때의 Forward값 벡터의 길이를 다시 1로 늘려주어 y에 대한 영향이 반영되게됨
        camForward.Normalize();
        camRight.Normalize();

        var moveInput = p_moveInput;

        if (moveInput.sqrMagnitude > 1) moveInput.Normalize();

        // 카메라 방향을 중심으로 방향 계산
        var moveDir = camRight * moveInput.x + camForward * moveInput.y;

        _inputMoveDir = moveDir;

        // 속력
        var speed = 0.0f;

        if (p_isCombat)
        {
            speed = _combatSpeed;
        }
        else
        {
            speed = p_isSprint ? _sprintSpeed : _moveSpeed;
        }
        _currentSpeed = speed;

        // 속도 계산
        Vector3 moveVelocity = moveDir * speed;

        _currentVelocity = moveVelocity + Vector3.up * _currentVelocityY;

        Rotate(_inputMoveDir, (_isJumping || _isDashing), p_isCombat);

        PlayCharacterController();

        _anim.MoveAnim(p_moveInput, p_isSprint, p_isCombat);
    }

    public void Rotate(Vector3 p_dir, bool instant, bool p_isCombat = false)
    {
        Vector3 targetDir;

        if (p_isCombat)
        {
            targetDir = Camera.main.transform.forward;
            targetDir.y = 0f;
        }
        else
        {
            targetDir = p_dir;
        }

        Quaternion targetRot = Quaternion.identity;

        if (targetDir != Vector3.zero)
            targetRot = Quaternion.LookRotation(targetDir);


        float targetAngle = targetRot.eulerAngles.y;
        float smoothedAngle = 0;

        if (instant)
        {
            transform.rotation = targetRot;
        }
        else
        { 
            smoothedAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _rotationSmoothVelocity, _rotationsmoothTime);
            transform.rotation = Quaternion.Euler(0f, smoothedAngle, 0f);
        }
    }

    public void CheckedGround()
    {
        if (!_enableGroundCheck)
        {
            _isGround = false;

            // 하강 시작
            if (_currentVelocityY <= 0f)
                _enableGroundCheck = true;

            return;
        }

        // _characterCtrl.center는 로컬좌표이기에 현재 월드 위치는 transform을 더해줘야한다.
        Vector3 center = transform.position + _characterCtrl.center;

        // radius를 해주는 이유는 캡슐형태에서 둥그런 부분의 높이도 함께 포함하여 감지하기 위해서
        float bottomOffset = (_characterCtrl.height * 0.5f) - _characterCtrl.radius;

        Vector3 ctrlBottom = center;
        ctrlBottom.y -= bottomOffset;
        ctrlBottom.y -= _checkGroundOffset;

        if (Physics.CheckSphere(ctrlBottom, _characterCtrl.radius * 1f, _groundLayer))
        {
            _isGround = true;
            _isJumping = false;
        }
        else _isGround = false;

    }

    private Vector3 ApplyGravity()
    {
        if (_isGround && _currentVelocityY < 0f)
        {
            // 바닥에 붙어있도록 약간의 음수값 유지
            if (_currentVelocityY < 0)
                _currentVelocityY = -0.5f;
        }
        else
        {
            // 중력 가속도 누적
            _currentVelocityY -= _gravity * Time.deltaTime;
        }

        return new Vector3(_currentVelocity.x, _currentVelocityY, _currentVelocity.z);
    }

    public void Jump()
    {
        _isJumping = true;
        _currentVelocityY = _jumpHeight;
        _enableGroundCheck = false;
        var dir = _currentVelocity;

        if (_inputMoveDir != Vector3.zero)
        {
            Rotate(_inputMoveDir, true);
        }

        _anim.JumpAnim();
    }

    public bool IsVelocityYZero()
    {
        if (_currentVelocityY <= 0f) return true;
        return false;
    }

    public void StartDash()
    {
        _isDashing = true;
        m_currentDashDistance = 0f;
        _dashDir = _inputMoveDir;

        if (_dashDir == Vector3.zero) _dashDir = transform.forward;
        _currentVelocityY = 0;
        _dashDir.Normalize();

        Rotate(_dashDir, true, false);

        _anim.DashAnim();
    }

    public void Dashing()
    {
        float dashSpeed = _dashDistance / _dashDuration;

        // 프레임당 이동 단위
        float moveStep = dashSpeed * Time.deltaTime;

        m_currentDashDistance += moveStep;

        // 거리 초과 방지
        if (m_currentDashDistance >= _dashDistance)
        {
            moveStep -= (m_currentDashDistance - _dashDistance);
            m_currentDashDistance = 0f;
            _isDashing = false;
        }

        _anim.IsDashingAnim(_isDashing);
        _currentVelocity = _dashDir * dashSpeed;

        PlayCharacterController();
    }

    private void OnDrawGizmos()
    {
        if (_characterCtrl == null)
            _characterCtrl = GetComponent<CharacterController>();

        if (_characterCtrl == null)
            return;

        Vector3 center = transform.position + _characterCtrl.center;

        float bottomOffset = (_characterCtrl.height * 0.5f) - _characterCtrl.radius;

        Vector3 ctrlBottom = center;
        ctrlBottom.y -= bottomOffset;
        ctrlBottom.y -= _checkGroundOffset;

        Gizmos.color = _isGround ? Color.green : Color.red;

        Gizmos.DrawWireSphere(ctrlBottom, _characterCtrl.radius * 1f);


        Debug.DrawLine(transform.position, ctrlBottom, Color.blue);
    }
}
