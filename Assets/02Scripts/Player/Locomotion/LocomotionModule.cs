using alpha.camera;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(CharacterController))]
public class LocomotionModule : MonoBehaviour
{
    // Ref
    private CharacterController characterCtrl;
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

    private float _rotationSmoothVelocity;
    private float _currentSpeed;
    private void Awake()
    {
        characterCtrl = GetComponent<CharacterController>();
        _anim = GetComponent<AnimationBoundary>();
    }

    private void Start()
    {
        _currentSpeed = _moveSpeed;
    }

    private void Update()
    {
        _isSprint = _inputBoundary.IsSprint;
        _isCombat = _inputBoundary.IsAttack;

        if (_isCombat) _isSprint = false;

        Move(_isSprint, _isCombat);
        //_cameraManager.SetBackViewFOV(_isCombat);
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
        Vector3 velocity = moveDir * speed * Time.deltaTime;

        Rotate(moveDir, p_isCombat);
        characterCtrl.Move(velocity);
        
        _anim.MoveAnim(moveInput, p_isSprint, p_isCombat);
    }

    public void Rotate(Vector3 p_dir, bool p_isCombat = false)
    {
        if (p_dir.magnitude < 0.01f) return;

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

    }

    private void ApplyGravity()
    {

    }
}
