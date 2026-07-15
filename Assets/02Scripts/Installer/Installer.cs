using UnityEngine;

public class Installer : MonoBehaviour
{
    [SerializeField] private PlayerCore _player;
    [SerializeField] private AlphaInputSystem _input;
    [SerializeField] private UIManager _ui;
    [SerializeField] private CameraCore _camera;

    [Header("Resource")]
    [SerializeField] private ResourceLoadSystem _resourceLoader;

    [Header("Inventory")]
    [SerializeField] private InventoryView _inventoryView;
    [SerializeField] private  EquipmentView _equipmentView;

    private InventoryPresenter _inventoryPresenter;
    private EquipmentPresenter _equipmentPresenter;
    private SlotTransferSystem _slotTransferSystem;

    private void Awake()
    {
        _player.Bind(_input, _ui, _camera);
    }

    private void Start()
    {
        _slotTransferSystem = new SlotTransferSystem();

        _inventoryPresenter =
            new InventoryPresenter(_player.InventoryModule, _inventoryView, _resourceLoader, _slotTransferSystem);

        _equipmentPresenter = 
            new EquipmentPresenter(_player.EquipmentModule, _equipmentView, _resourceLoader, _slotTransferSystem);

        _inventoryPresenter.Initialize();
        _equipmentPresenter.Initialize();

        _player.InventoryFlow.BindPresenter(_inventoryPresenter);
    }
}
