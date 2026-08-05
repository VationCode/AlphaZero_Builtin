using Alpha.AlphaCamera;
using Alpha.Mouse;
using Alpha.Player;
using Alpha.Player.Inventory;
using Alpha.UI;
using Unity.VisualScripting;
using UnityEngine;

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

    private void Awake()
    {
        // 외부 의존성만 Player에 연결한다.
        _playerCore.Bind(_input, _cameraCore, _mouseSystem, _itemDatabase);

        // Playerd의 Locomotion, Combat과 카메라 ViewType을 UI View에 연결한다.
        _playerCore.LocomotionContext.OnStateChanged += _uiManager.StateUI.ChangeLocoState;
        _playerCore.CombatContext.OnStateChanged += _uiManager.StateUI.ChangeCombatState;
        //_cameraCore.Context.OnCameraViewChanged += _uiManager.StateUI.ChangeViewType;
    }

    private async void Start()
    {
        // Camera는 Item Database와 무관하므로 즉시 연결한다.
        if (_cameraCore.Bind(_input))
        {
            _mouseSystem.Bind(_cameraCore.RenderCamera);

            //_cameraCore.OnBaseViewChanged += viewType => _mouseSystem.SetViewCursor(viewType == ECameraViewType.Quarter);

            // 초기 View는 즉시 적용한다.
            _cameraCore.RequestView(ECameraViewType.ThirdPerson, 0f);

            // 시작 View는 TPS이므로 커서를 잠근다.
            _mouseSystem.SetViewCursor(false);
        }

        await _itemDatabase.InitializeAsync();

        _inventoryView.Bind(_playerCore.InventoryContext, _resourceLoader);

        ConnectInventoryEvents();
    }

    private void ConnectInventoryEvents()
    {
        InventoryFlow flow = _playerCore.InventoryFlow;

        // 슬롯 추가 요청
        _inventoryView.OnAddSlotRequested -= flow.RequestAddSlot;
        _inventoryView.OnAddSlotRequested += flow.RequestAddSlot;

        // 슬롯 간 아이템 이전 요청
        _inventoryView.OnTransferRequested -= flow.RequestTransferItem;
        _inventoryView.OnTransferRequested += flow.RequestTransferItem;

        // 인벤토리 화면 상태
        flow.OnViewStateChanged -= HandleInventoryStateChanged;
        flow.OnViewStateChanged += HandleInventoryStateChanged;

        _inventoryView.OnCloseInventoryRequested -= flow.RequestCloseInventory;
        _inventoryView.OnCloseInventoryRequested += flow.RequestCloseInventory;

        _inventoryView.OnPageRequested -= flow.RequestOpenPage;
        _inventoryView.OnPageRequested += flow.RequestOpenPage;
        
        // 이벤트 연결 전의 현재 상태도 반영한다.
        HandleInventoryStateChanged(flow.IsOpen, flow.CurrentWindow);
    }


    private void HandleInventoryStateChanged(bool p_isInventoryOpen, EItemType p_pageType)
    {
        // 인벤토리 UI 상태 적용
        _inventoryView.ApplyViewState(p_isInventoryOpen, p_pageType);

        // 인벤토리가 열리면 커서를 표시하고 잠금을 해제한다.
        _mouseSystem.SetUICursor(p_isInventoryOpen);
    }

    private void OnDestroy()
    {
        if (_playerCore == null || _playerCore.InventoryFlow == null)
        {
            return;
        }

        InventoryFlow flow = _playerCore.InventoryFlow;

        _inventoryView.OnAddSlotRequested -= flow.RequestAddSlot;
        _inventoryView.OnTransferRequested -= flow.RequestTransferItem;
        _inventoryView.OnCloseInventoryRequested -= flow.RequestCloseInventory;
        _inventoryView.OnPageRequested -= flow.RequestOpenPage;
        flow.OnViewStateChanged -= HandleInventoryStateChanged;
    }
}