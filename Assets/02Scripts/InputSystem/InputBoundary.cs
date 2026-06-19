using UnityEngine;

public class InputBoundary : MonoBehaviour
{
    private InputSystem_Actions _action;

    public Vector2 MoveInput => _moveInput;
    Vector2 _moveInput;

    public bool IsSprint => _isSprint;
    bool _isSprint;

    public Vector2 LookInput => _lookInput;
    Vector2 _lookInput;
    public Vector2 MouseScroll => _mouseScroll;
    Vector2 _mouseScroll;

    public bool IsAttack => _isAttack;
    private bool _isAttack;
    private void OnEnable()
    {
        _action = new InputSystem_Actions();

        _action.Player.Move.performed += i => _moveInput = i.ReadValue<Vector2>();
        _action.Player.Move.canceled += i => _moveInput = Vector2.zero;

        _action.Player.Sprint.performed += i => _isSprint = true;
        _action.Player.Sprint.canceled += i => _isSprint = false;

        _action.Camera.Look.performed += i => _lookInput = i.ReadValue<Vector2>();
        _action.Camera.Look.canceled += i => _lookInput = Vector2.zero;

        _action.Camera.MouseScroll.performed += i => _mouseScroll = i.ReadValue<Vector2>();
        _action.Camera.MouseScroll.canceled += i => _mouseScroll = Vector2.zero;


        // Combat
        _action.Player.Attack.performed += i => _isAttack = true;
        _action.Player.Attack.canceled += i => _isAttack = false;

        // 활성화해야 동작
        _action.Enable();
    }
}
