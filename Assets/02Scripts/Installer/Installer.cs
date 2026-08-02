using Alpha.AlphaCamera;
using Alpha.Equipment;
using Alpha.Inventory;
using Alpha.Mouse;
using Alpha.Player;
using Alpha.UI;
using UnityEngine;

public class Installer : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private PlayerCore _playerCore;

    [Header("Input")]
    [SerializeField] private AlphaInputSystem _input;

    [Header("Camera")]
    [SerializeField] private CameraCore _cameraCore;

    [Header("Inventory")]
    [SerializeField] private InventoryCore _inventoryCore;

    [Header("Equipment")]
    [SerializeField] private EquipmentCore _equipmentCore;

    [Header("Data")]
    [SerializeField] private ResourceLoadSystem _resourceLoader;
    [SerializeField] private ItemDatabaseManager _itemDatabase;

    [Header("Mouse")]
    [SerializeField] private MouseSystem _mouseSystem;

    [Header("UI")]
    [SerializeField] private UIManager _uiManager;

    private void Awake()
    {
        // 외부 의존성만 Player에 연결한다.
        _playerCore.Bind(_input, _cameraCore, _mouseSystem);

        // Playerd의 Locomotion, Combat과 카메라 ViewType을 UI View에 연결한다.
        _playerCore.LocomotionContext.OnStateChanged += _uiManager.StateUI.ChangeLocoState;
        _playerCore.CombatContext.OnStateChanged += _uiManager.StateUI.ChangeCombatState;
        _cameraCore.Context.OnCameraViewChanged += _uiManager.StateUI.ChangeViewType;
    }

    private async void Start()
    {
        // Camera는 Item Database와 무관하므로 즉시 연결한다.
        if (_cameraCore.Bind(_input, _playerCore.transform))
        {
            _mouseSystem.Bind(_cameraCore.RenderCamera);

            _cameraCore.OnBaseViewChanged += viewType => _mouseSystem.SetViewCursor(viewType == ECameraViewType.Quarter);

            // 시작 View는 TPS이므로 커서를 잠근다.
            _mouseSystem.SetViewCursor(false);
        }

        await _itemDatabase.InitializeAsync();

        // UI Entity 내부 상태 초기화
        // Inventory 상태와 View 연결을 먼저 완료한다.
        if (!_inventoryCore.Bind(_input, _resourceLoader))
        {
            Debug.LogError("Inventory 초기화에 실패했습니다.", this);
            return;
        }

        // Inventory 창 상태를 외부 시스템에 연결한다.(마우스 활성, Combat 입력 제어)
        _inventoryCore.OnWindowActiveChanged += _mouseSystem.SetUICursor;
        _inventoryCore.OnWindowActiveChanged += _playerCore.SetCombatBlocked;

        // 아이템 습득 대상을 초기화된 Inventory에 연결한다.
        _playerCore.ItemPickupFlow.Bind(_inventoryCore, _itemDatabase);

        // Equipment UI는 Inventory Presenter가 생성된 후 연결한다.
        if (!_equipmentCore.Bind(_resourceLoader, _inventoryCore.Presenter))
        {
            Debug.LogError("Equipment 초기화에 실패했습니다.", this);
            return;
        }

        // Player 장비 상태와 실제 장비 외형을 연결한다.
        if (!_playerCore.BindEquipment(_resourceLoader))
        {
            Debug.LogError("Player Equipment Module 연결에 실패했습니다.", this);
            return;
        }

        if (!_playerCore.EquipmentFlow.Bind(_equipmentCore, _playerCore.EquipmentModule))
        {
            Debug.LogError("Player Equipment Flow 연결에 실패했습니다.", this);
            return;
        }


        // Equipment가 준비된 이후 Combat 내부 Module을 조립한다.
        if (!_playerCore.BindCombat())
        {
            Debug.LogError("Player Combat Module 연결에 실패했습니다.", this);
            return;
        }
        // Equipment 상태를 Player의 CombatFlow에 연결한다.
        _playerCore.CombatFlow.Bind(_playerCore);

    }

    private void OnDestroy()
    {
        if (_inventoryCore == null) return;

        _inventoryCore.OnWindowActiveChanged -= _mouseSystem.SetUICursor;
        _inventoryCore.OnWindowActiveChanged -= _playerCore.SetCombatBlocked;
    }
}

// Equipment 흐름
/*
 EquipmentCore.OnEquippedArmorChanged
              ↓
PlayerEquipmentFlow
              ↓
PlayerEquipmentModule
              ↓
PlayerEquipmentView
 */

/* 무기 변경 시 Equipment 흐름
Equipment Slot 변경
→ PlayerEquipmentFlow
→ PlayerEquipmentModule
→ 무기 상태·외형·Animator 변경
 */
