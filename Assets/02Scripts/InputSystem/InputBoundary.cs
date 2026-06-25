using UnityEngine;

public class InputBoundary : MonoBehaviour
{
    private InputSystem_Actions _action;
    #region ==================== Player
    // Locomotion
    public Vector2 MoveInput => _moveInput;
    Vector2 _moveInput;

    public bool IsSprint => _isSprint;
    bool _isSprint;

    public bool IsJump => _isJump;
    private bool _isJump => _jumpFrame == Time.frameCount;  // 한 프레임 단위만 True로 이후 False
    private int _jumpFrame;

    public bool IsDash => _isDash;
    private bool _isDash => _dashFrame == Time.frameCount;
    private int _dashFrame;

    // Combat
    public bool IsAttack => _isAttack;
    private bool _isAttack;

    public bool IsAim => _isAim;
    private bool _isAim;
    #endregion ==================== /Player

    #region ==================== Camera
    public Vector2 LookInput => _lookInput;
    Vector2 _lookInput;
    public Vector2 MouseScroll => _mouseScroll;
    Vector2 _mouseScroll;


    public Vector3 MouseInputPos => _mousePos;
    private Vector3 _mousePos;
    #endregion ==================== /Camera
    private void OnEnable()
    {
        _action = new InputSystem_Actions();

        // Locomotion
        _action.Player.Move.performed += i => _moveInput = i.ReadValue<Vector2>();
        _action.Player.Move.canceled += i => _moveInput = Vector2.zero;

        _action.Player.Sprint.performed += i => _isSprint = true;
        _action.Player.Sprint.canceled += i => _isSprint = false;

        _action.Player.Jump.performed += i => _jumpFrame = Time.frameCount;
        _action.Player.Dash.performed += i => _dashFrame = Time.frameCount;

        // Combat
        _action.Player.Attack.performed += i => _isAttack = true;
        _action.Player.Attack.canceled += i => _isAttack = false;

        _action.Player.Aim.performed += i => _isAim = true;
        _action.Player.Aim.canceled += i => _isAim = false;

        // Camera
        _action.Camera.Look.performed += i => _lookInput = i.ReadValue<Vector2>();
        _action.Camera.Look.canceled += i => _lookInput = Vector2.zero;

        _action.Camera.MouseScroll.performed += i => _mouseScroll = i.ReadValue<Vector2>();
        _action.Camera.MouseScroll.canceled += i => _mouseScroll = Vector2.zero;


        _action.Camera.MousePos.performed += i => _mousePos = i.ReadValue<Vector2>();
        _action.Camera.MousePos.canceled += i => _mousePos = Vector2.zero;

        // 활성화해야 동작
        _action.Enable();
    }
}
