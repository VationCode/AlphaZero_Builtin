using Alpha.AlphaCamera;
using Alpha.Mouse;
using Alpha.Player;
using Alpha.Player.Equipment;
using Alpha.Player.Inventory;
using Alpha.UI;
using UnityEngine;

// Scene의 Entity·System·UI 의존성을 조립하고 초기화 순서를 관리한다.
public class Installer : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private PlayerCore _playerCore;

    [Header("Input")]
    [SerializeField] private AlphaInputSystem _input;

    [Header("Camera")]
    [SerializeField] private CameraCore _cameraCore;

    [Header("Data")]
    [SerializeField] private ResourceLoadSystem _resourceLoader;
    [SerializeField] private ItemDatabaseManager _itemDatabase;

    [Header("Mouse")]
    [SerializeField] private MouseSystem _mouseSystem;

    [Header("UI")]
    [SerializeField] private UIManager _uiManager;
    [SerializeField] private InventoryView _inventoryView;
    [SerializeField] private EquipmentView _equipmentView;

    // Player에 외부 참조를 전달하고 상태 표시 UI 이벤트를 연결한다.
    private void Awake()
    {
        _playerCore.Bind(
            _input,
            _cameraCore,
            _mouseSystem,
            _itemDatabase);

        _playerCore.LocomotionContext.OnStateChanged +=
            _uiManager.StateUI.ChangeLocoState;

        _playerCore.CombatContext.OnStateChanged +=
            _uiManager.StateUI.ChangeCombatState;
    }

    // 카메라 → 데이터 → 인벤토리·장비 View 순서로 비동기 초기화한다.
    private async void Start()
    {
        // 카메라가 준비된 경우 마우스와 기본 시점을 함께 설정한다.
        if (_cameraCore.Bind(_input))
        {
            _mouseSystem.Bind(_cameraCore.RenderCamera);
            _cameraCore.RequestView(
                ECameraViewType.ThirdPerson,
                0f);
            _mouseSystem.SetViewCursor(false);
        }

        // 아이템 데이터가 준비된 뒤 이를 사용하는 UI를 연결한다.
        await _itemDatabase.InitializeAsync();

        _inventoryView.Bind(
            _playerCore.InventoryContext,
            _resourceLoader,
            _playerCore.InventoryFlow,
            _playerCore.EquipmentFlow);

        _equipmentView.Bind(
            _playerCore.EquipmentContext,
            _resourceLoader,
            _playerCore.EquipmentFlow);

        // 최초 화면 상태까지 한 번 적용한다.
        ConnectInventoryState();
    }

    // InventoryFlow 상태 변경을 UI와 마우스 커서에 연결한다.
    private void ConnectInventoryState()
    {
        InventoryFlow flow = _playerCore.InventoryFlow;

        // 중복 구독을 방지한 뒤 현재 상태를 즉시 반영한다.
        flow.OnViewStateChanged -= HandleInventoryStateChanged;
        flow.OnViewStateChanged += HandleInventoryStateChanged;

        HandleInventoryStateChanged(
            flow.IsOpen,
            flow.CurrentWindow);
    }

    // 인벤토리 화면 활성 상태와 UI 커서 모드를 함께 갱신한다.
    private void HandleInventoryStateChanged(
        bool p_isInventoryOpen,
        EItemType p_pageType)
    {
        _inventoryView.ApplyViewState(
            p_isInventoryOpen,
            p_pageType);

        _mouseSystem.SetUICursor(p_isInventoryOpen);
    }

    // 객체 해제 시 등록한 이벤트와 참조를 정리한다.
    private void OnDestroy()
    {
        if (_playerCore?.InventoryFlow != null)
        {
            _playerCore.InventoryFlow.OnViewStateChanged -=
                HandleInventoryStateChanged;
        }
    }
}
