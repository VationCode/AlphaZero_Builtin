using UnityEngine;
using UnityEngine.InputSystem;

// Input System Callback을 게임에서 사용하기 쉬운 현재 값과 1 Frame 입력으로 변환한다.
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

    // Input Action을 생성하고 Player·Camera·UI 입력 Callback을 연결한다.
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
    // 직접 등록한 Callback과 입력 상태를 정리한 뒤 Input Action을 해제한다.
    private void OnDisable()
    {
        if (_action == null)
            return;

        // Method Group으로 직접 연결한 Callback은 명시적으로 구독 해제한다.
        _action.Player.Swap.performed -= OnSwap;
        _action.Camera.Quarter.performed -= OnQuarter;

        // 비활성화 중 이전 입력이 남지 않도록 지속 입력 상태를 초기화한다.
        _moveInput = Vector2.zero;
        _isSprint = false;
        _isPrimaryAction = false;
        _isSecondaryAction = false;
        _isQuarter = false;

        m_swapFrame = -1;

        // 생성한 Input Action Wrapper의 생명주기를 함께 종료한다.
        _action.Disable();
        _action.Dispose();
        _action = null;
    }

    // 숫자키 표시 이름을 장비 슬롯 번호와 1 Frame 교체 입력으로 변환한다.
    private void OnSwap(InputAction.CallbackContext p_context)
    {
        string key = p_context.control.displayName;

        if (int.TryParse(key, out int number))
        {
            SwapNum = number - 1;
            m_swapFrame = Time.frameCount;
        }
    }


    // Quarter Camera 시험 입력마다 활성 상태를 전환한다.
    private void OnQuarter(InputAction.CallbackContext p_context)
    {
        _isQuarter = !_isQuarter;
    }
}
