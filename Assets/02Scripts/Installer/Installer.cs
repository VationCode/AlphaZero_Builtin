using Alpha.AlphaCamera;
using Alpha.Enemy;
using Alpha.Gameplay;
using Alpha.Mouse;
using Alpha.Player;
using Alpha.Player.Combat;
using Alpha.Player.Equipment;
using Alpha.Player.Inventory;
using Alpha.UI;
using Unity.Cinemachine;
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

    [Header("Gameplay / Boss")]
    [SerializeField] private GameplayPauseSystem _gameplayPauseSystem;
    [SerializeField] private CrabBossEncounterFlow _crabBossEncounterFlow;

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
        _playerCore.Bind(_input, _cameraCore, _mouseSystem, _itemDatabase, _resourceLoader);

        _gameplayPauseSystem?.Bind(_input);
        _crabBossEncounterFlow?.Bind(
            _playerCore,
            _input);

        if (_crabBossEncounterFlow != null && _uiManager != null)
        {
            _crabBossEncounterFlow.OnGameplayHudVisibilityRequested -=
                _uiManager.SetGameplayHudVisible;
            _crabBossEncounterFlow.OnGameplayHudVisibilityRequested +=
                _uiManager.SetGameplayHudVisible;
        }

        _playerCore.LocomotionContext.OnStateChanged += _uiManager.StateUI.ChangeLocoState;

        _playerCore.CombatContext.OnStateChanged += _uiManager.StateUI.ChangeCombatState;
    }

    // 카메라 → 데이터 → 인벤토리·장비 View 순서로 비동기 초기화한다.
    private async void Start()
    {
        _uiManager.InteractionUI?.Bind(_playerCore.ItemPickupFlow);
        _uiManager.RangeChargeGaugeView?.Bind(
            _playerCore.CombatModule);

        // 카메라가 준비된 경우 마우스와 기본 시점을 함께 설정한다.
        if (_cameraCore.Bind(_input))
        {
            _crabBossEncounterFlow?.BindCamera(
                _cameraCore.RenderCamera?.GetComponent<CinemachineBrain>());

            _uiManager.CrossHairUI?.Bind(
                _cameraCore,
                _playerCore.CombatModule);
            ConnectCrossHairState();
            _mouseSystem.Bind(_cameraCore.RenderCamera);
            _cameraCore.RequestView(ECameraViewType.ThirdPerson);
            _mouseSystem.SetViewCursor(false);
        }

        // 아이템 데이터가 준비된 뒤 이를 사용하는 UI를 연결한다.
        await _itemDatabase.InitializeAsync();

        _inventoryView.Bind(_playerCore.InventoryContext, _resourceLoader);

        _equipmentView.Bind(_playerCore.EquipmentContext, _resourceLoader);

        // View 요청과 Flow를 연결한 뒤 최초 화면 상태를 적용한다.
        ConnectViewRequests();
        ConnectInventoryState();
    }

    // View가 발행한 사용자 요청을 담당 Flow에 직접 연결한다.
    private void ConnectViewRequests()
    {
        InventoryFlow inventoryFlow = _playerCore.InventoryFlow;

        EquipmentFlow equipmentFlow = _playerCore.EquipmentFlow;

        // 재연결 시에도 같은 요청이 중복 실행되지 않도록 먼저 해제한다.
        _inventoryView.OnPageRequested -= inventoryFlow.RequestOpenPage;
        _inventoryView.OnPageRequested += inventoryFlow.RequestOpenPage;

        _inventoryView.OnCloseRequested -= inventoryFlow.RequestCloseInventory;
        _inventoryView.OnCloseRequested += inventoryFlow.RequestCloseInventory;

        _inventoryView.OnAddSlotRequested -= inventoryFlow.RequestAddSlot;
        _inventoryView.OnAddSlotRequested += inventoryFlow.RequestAddSlot;

        _inventoryView.OnTransferRequested -= inventoryFlow.RequestTransferItem;
        _inventoryView.OnTransferRequested += inventoryFlow.RequestTransferItem;

        _inventoryView.OnEquipRequested -= equipmentFlow.RequestEquip;
        _inventoryView.OnEquipRequested += equipmentFlow.RequestEquip;

        _inventoryView.OnUnequipRequested -= equipmentFlow.RequestUnequip;
        _inventoryView.OnUnequipRequested += equipmentFlow.RequestUnequip;

        _equipmentView.OnEquipRequested -= equipmentFlow.RequestEquip;
        _equipmentView.OnEquipRequested += equipmentFlow.RequestEquip;

        _equipmentView.OnUnequipRequested -= equipmentFlow.RequestUnequip;
        _equipmentView.OnUnequipRequested += equipmentFlow.RequestUnequip;
    }

    // 실제 활성 무기 변경을 CrossHair 표시 조건과 연결한다.
    private void ConnectCrossHairState()
    {
        CrossHairUI crossHairUI = _uiManager?.CrossHairUI;
        CombatModule combatModule = _playerCore?.CombatModule;

        if (crossHairUI == null || combatModule == null)
            return;

        combatModule.OnWeaponChanged -= crossHairUI.HandleWeaponChanged;
        combatModule.OnWeaponChanged += crossHairUI.HandleWeaponChanged;

        crossHairUI.HandleWeaponChanged(
            combatModule.CurrentWeapon?.Data);
    }

    // 객체 해제 전에 View 요청과 Flow의 연결을 모두 해제한다.
    private void DisconnectViewRequests()
    {
        if (_playerCore == null)
            return;

        InventoryFlow inventoryFlow = _playerCore.InventoryFlow;

        EquipmentFlow equipmentFlow = _playerCore.EquipmentFlow;

        if (_inventoryView != null)
        {
            _inventoryView.OnPageRequested -= inventoryFlow.RequestOpenPage;
            _inventoryView.OnCloseRequested -= inventoryFlow.RequestCloseInventory;
            _inventoryView.OnAddSlotRequested -= inventoryFlow.RequestAddSlot;
            _inventoryView.OnTransferRequested -= inventoryFlow.RequestTransferItem;
            _inventoryView.OnEquipRequested -= equipmentFlow.RequestEquip;
            _inventoryView.OnUnequipRequested -= equipmentFlow.RequestUnequip;
        }

        if (_equipmentView != null)
        {
            _equipmentView.OnEquipRequested -= equipmentFlow.RequestEquip;
            _equipmentView.OnUnequipRequested -= equipmentFlow.RequestUnequip;
        }

        if (_uiManager?.CrossHairUI != null &&
            _playerCore.CombatModule != null)
        {
            _playerCore.CombatModule.OnWeaponChanged -=
                _uiManager.CrossHairUI.HandleWeaponChanged;
        }
    }

    // InventoryFlow 상태 변경을 UI, 커서, Combat 입력 차단에 연결한다.
    private void ConnectInventoryState()
    {
        InventoryFlow flow = _playerCore.InventoryFlow;

        // 중복 구독을 방지한 뒤 현재 상태를 즉시 반영한다.
        flow.OnViewStateChanged -= HandleInventoryStateChanged;
        flow.OnViewStateChanged += HandleInventoryStateChanged;

        HandleInventoryStateChanged(flow.IsOpen, flow.CurrentWindow);
    }

    // 인벤토리 화면 활성 상태와 Player 입력 제약을 함께 갱신한다.
    private void HandleInventoryStateChanged(bool p_isInventoryOpen, EItemType p_pageType)
    {
        // 인벤토리가 열려 있으면 Combat 입력을 차단한다.
        _playerCore.SetCombatBlocked(p_isInventoryOpen);

        _inventoryView.ApplyViewState(p_isInventoryOpen, p_pageType);

        _mouseSystem.SetUICursor(p_isInventoryOpen);
    }

    // 객체 해제 시 등록한 이벤트와 참조를 정리한다.
    private void OnDestroy()
    {
        _uiManager?.InteractionUI?.Unbind();
        _uiManager?.RangeChargeGaugeView?.Unbind();
        DisconnectViewRequests();

        if (_crabBossEncounterFlow != null && _uiManager != null)
        {
            _crabBossEncounterFlow.OnGameplayHudVisibilityRequested -=
                _uiManager.SetGameplayHudVisible;
        }

        if (_playerCore?.InventoryFlow != null)
        {
            _playerCore.InventoryFlow.OnViewStateChanged -= HandleInventoryStateChanged;
        }
    }
}
