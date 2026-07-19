using Alpha.AlphaCamera;
using Alpha.Inventory;
using Alpha.Mouse;
using Alpha.Player;
using UnityEngine;

public class Installer : MonoBehaviour
{
    [SerializeField] private PlayerCore _player;
    [SerializeField] private AlphaInputSystem _input;
    [SerializeField] private UIManager _ui;
    [SerializeField] private CameraCore _cameraCore;

    [Header("Resource")]
    [SerializeField] private ResourceLoadSystem _resourceLoader;

    [Header("Inventory")]
    [SerializeField] private InventoryView _inventoryView;
    [SerializeField] private  EquipmentView _equipmentView;

    [Header("Mouse")]
    [SerializeField] private MouseSystem _mouseSystem;

    private InventoryPresenter _inventoryPresenter;
    private EquipmentPresenter _equipmentPresenter;
    private SlotTransferSystem _slotTransferSystem;
    private DamageSystem _damageSystem;
    private void Awake()
    {
        _damageSystem = new DamageSystem();

        // 외부 의존성만 Player에 연결한다.
        _player.Bind(_input,_ui,_cameraCore,_resourceLoader,_damageSystem,_mouseSystem);
    }

    private void Start()
    {
        _cameraCore.Bind(_input, _player.transform, _mouseSystem);
        _mouseSystem.Bind(_cameraCore.RenderCamera);

        _slotTransferSystem = 
            new SlotTransferSystem();

        _inventoryPresenter = 
            new InventoryPresenter(_player.InventoryModule,_inventoryView,_resourceLoader,_slotTransferSystem);

        _equipmentPresenter = 
            new EquipmentPresenter(_player.EquipmentModule, _equipmentView, _resourceLoader, _slotTransferSystem);

        _inventoryPresenter.Initialize();
        _equipmentPresenter.Initialize();

        _player.InventoryFlow.Bind(_player, _inventoryPresenter);
    }
}
