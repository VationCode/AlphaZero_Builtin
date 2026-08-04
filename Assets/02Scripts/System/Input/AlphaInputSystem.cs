using UnityEngine;
using UnityEngine.InputSystem;

public class AlphaInputSystem : MonoBehaviour
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

    public bool IsFlight => _isFlight;
    private bool _isFlight => _flightFrame == Time.frameCount;
    private int _flightFrame;

    // Combat
    public bool IsPrimaryAction => _isPrimaryAction;
    private bool _isPrimaryAction;

    public bool IsSecondaryAction => _isSecondaryAction;
    private bool _isSecondaryAction;

    public int SwapNum { get; private set; }
    public bool IsSwapInput => m_swapFrame == Time.frameCount;
    private int m_swapFrame;

    // Camera Test
    public bool IsQuarter => _isQuarter;
    private bool _isQuarter;

    #endregion ==================== /Player

    #region ==================== Camera
    public Vector2 LookInput => _lookInput;
    Vector2 _lookInput;
    public Vector2 MouseScroll => _mouseScroll;
    Vector2 _mouseScroll;


    public Vector3 MouseInputPos => _mousePos;
    private Vector3 _mousePos;
    #endregion ==================== /Camera

    #region ==================== UI
    public bool IsInventory => _isInventory;
    private bool _isInventory => _inventoryFrame == Time.frameCount;
    private int _inventoryFrame;
    #endregion ==================== /UI

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
        _action.Player.Flight.performed += i => _flightFrame = Time.frameCount;

        // Combat
        _action.Player.PrimaryAction.performed += i => _isPrimaryAction = true;
        _action.Player.PrimaryAction.canceled += i => _isPrimaryAction = false;

        _action.Player.SecondaryAction.performed += i => _isSecondaryAction = true;
        _action.Player.SecondaryAction.canceled += i => _isSecondaryAction = false;

        _action.Player.Swap.performed += OnSwap;

        // Camera
        _action.Camera.Look.performed += i => _lookInput = i.ReadValue<Vector2>();
        _action.Camera.Look.canceled += i => _lookInput = Vector2.zero;

        _action.Camera.MouseScroll.performed += i => _mouseScroll = i.ReadValue<Vector2>();
        _action.Camera.MouseScroll.canceled += i => _mouseScroll = Vector2.zero;

        _action.Camera.MousePos.performed += i => _mousePos = i.ReadValue<Vector2>();
        _action.Camera.MousePos.canceled += i => _mousePos = Vector2.zero;

        _action.UI.Inventory.performed += i => _inventoryFrame = Time.frameCount;

        // 테스트 중에는 Q를 누르고 있는 동안 QuarterView를 사용한다.
        _action.Camera.Quarter.performed += OnQuarter;

        // 활성화해야 동작
        _action.Enable();
    }
    private void OnDisable()
    {
        if (_action == null)
            return;

        _action.Player.Swap.performed -= OnSwap;
        _action.Camera.Quarter.performed -= OnQuarter;

        _moveInput = Vector2.zero;
        _isSprint = false;
        _isPrimaryAction = false;
        _isSecondaryAction = false;
        _isQuarter = false;

        m_swapFrame = -1;

        _action.Disable();
        _action.Dispose();
        _action = null;
    }

    // Numpad 대응
    private void OnSwap(InputAction.CallbackContext p_context)
    {
        string key = p_context.control.displayName;

        if (int.TryParse(key, out int number))
        {
            SwapNum = number - 1;
            m_swapFrame = Time.frameCount;
        }
    }


    private void OnQuarter(InputAction.CallbackContext p_context)
    {
        _isQuarter = !_isQuarter;
    }
}
