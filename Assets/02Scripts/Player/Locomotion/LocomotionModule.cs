using alpha.camera;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(CharacterController))]
public class LocomotionModule : MonoBehaviour
{
    // Ref
    private CharacterController _characterCtrl;
    private AnimationBoundary _anim;
    [SerializeField]
    private InputBoundary _inputBoundary;
    [SerializeField]
    private CameraManager _cameraManager;


    [SerializeField]
    private EViewType _viewType;

    [SerializeField]
    private float _moveSpeed = 4f;
    [SerializeField]
    private float _sprintSpeed = 7f;
    [SerializeField]
    private float _combatSpeed = 2.5f;

    [SerializeField]
    private float _rotationsmoothTime = 0.1f;

    [SerializeField]
    private bool _isCombat = false;
    [SerializeField]
    private bool _isSprint;

    [Header("[Ground]"), SerializeField]
    private LayerMask _groundLayer;
    [SerializeField]
    private float _checkGroundOffset = 0.125f;
    private bool _isGround;

    [Header("[Gravity]"), SerializeField]
    private float _gravity = -9.8f;

    private float _rotationSmoothVelocity;
    private float _currentSpeed;
    private Vector3 _currentVelocity;
    private float _currentVelocityY;
    private bool _isCombatStart = false;
    private bool _isAim = false;

    private void Awake()
    {
        _characterCtrl = GetComponent<CharacterController>();
        _anim = GetComponent<AnimationBoundary>();
    }

    private void Start()
    {
        _currentSpeed = _moveSpeed;
    }

    private void Update()
    {
        _isSprint = _inputBoundary.IsSprint;

        _isAim = _inputBoundary.IsAim;
        _isCombat = _inputBoundary.IsAttack || _isAim;

        if (_isCombat)
        {
            if (!_isCombatStart)
            {
                if(_isAim)
                    _cameraManager.SetView(EViewType.ShoulderView);
                
                _isSprint = false;
                _isCombatStart = true;
            }
        }
        else
        {
            if(_isCombatStart)
            {
                _cameraManager.SetView(EViewType.BackView);
                _isCombatStart = false;
            }
        }

        CheckedGround();

        ApplyGravity();
        
        Move(_isSprint, _isCombat);
    }

    public void Move(bool p_isSprint = false, bool p_isCombat = false)
    {
        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;

        camForward.y = 0f;
        camRight.y = 0f;

        // 바닥이나 하늘을 바라봤을때의 Forward값 벡터의 길이를 다시 1로 늘려주어 y에 대한 영향이 반영되게됨
        camForward.Normalize();
        camRight.Normalize();

        var moveInput = _inputBoundary.MoveInput;

        if (moveInput.sqrMagnitude > 1) moveInput.Normalize();
        
        // 카메라 방향을 중심으로 방향 계산
        var moveDir = camRight * moveInput.x + camForward * moveInput.y;

        var speed = _currentSpeed;

        if (p_isCombat)
        {
            speed = _combatSpeed;
        }
        else
        {
            speed = p_isSprint ? _sprintSpeed : _moveSpeed;
        }

        // 속도 계산
        
        Vector3 moveVelocity = moveDir * speed;

        Rotate(moveDir, p_isCombat);


        Vector3 finalVelocity = moveVelocity + Vector3.up * _currentVelocityY;


        _characterCtrl.Move(finalVelocity * Time.deltaTime);
        
        _anim.MoveAnim(moveInput, p_isSprint, p_isCombat);
    }

    public void Rotate(Vector3 p_dir, bool p_isCombat = false)
    {
        //if (p_dir.magnitude < 0.01f) return;

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

        Quaternion targetRot = Quaternion.LookRotation(targetDir);

        float targetAngle = targetRot.eulerAngles.y;

        float smoothedAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _rotationSmoothVelocity, _rotationsmoothTime);
        
        transform.rotation = Quaternion.Euler(0f, smoothedAngle, 0f);
    }

    private void CheckedGround()
    {
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
        }
        else _isGround = false;
    }

    private void ApplyGravity()
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
            _currentVelocityY += _gravity * Time.deltaTime;
        }
    }

    public void Jump()
    {

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
    }

}
