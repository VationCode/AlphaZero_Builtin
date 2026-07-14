using UnityEngine;

public class Installer : MonoBehaviour
{
    [SerializeField] private PlayerCore _player;
    [SerializeField] private AlphaInputSystem _input;
    [SerializeField] private UIManager _ui;
    [SerializeField] private CameraCore _camera;
    [SerializeField] private InventoryView _inventoryView;

    [SerializeField]
    private ResourceLoadSystem _resourceLoader;

    private InventoryPresenter _inventoryPresenter;
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

        _inventoryPresenter.Initialize();
        _player.InventoryFlow.BindPresenter(_inventoryPresenter);
    }
}
