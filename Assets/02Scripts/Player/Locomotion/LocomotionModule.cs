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
    float _moveSpeed = 3f;
    [SerializeField]
    float _rotationsmoothTime = 0.1f;

    float _rotationSmoothVelocity;
    private void Awake()
    {
        characterCtrl = GetComponent<CharacterController>();
        _anim = GetComponent<AnimationBoundary>();
    }

    private void FixedUpdate()
    {
        Move();
    }

    public void Move()
    {
        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;

        camForward.y = 0f;
        camRight.y = 0f;

        var moveInput = _inputBoundary.MoveInput;

        if (moveInput.sqrMagnitude > 1) moveInput.Normalize();

        // 카메라 방향을 중심으로 방향 계산
        var moveDir = camRight * moveInput.x + camForward * moveInput.y;

        // 속도 계산
        Vector3 velocity = moveDir * _moveSpeed * Time.fixedDeltaTime;

        Rotate(moveDir);
        characterCtrl.Move(velocity);

        _anim.MoveAnim(moveInput);
    }

    public void Rotate(Vector3 p_dir)
    {
        Quaternion targetRot = Quaternion.LookRotation(p_dir);

        float targetAngle = targetRot.eulerAngles.y;

        float smoothedAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _rotationSmoothVelocity, _rotationsmoothTime);
        
        transform.rotation = Quaternion.Euler(0f, smoothedAngle, 0f);
    }
}
