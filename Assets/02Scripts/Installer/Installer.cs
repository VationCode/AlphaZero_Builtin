using UnityEngine;

public class Installer : MonoBehaviour
{
    [SerializeField] private PlayerCore _player;
    [SerializeField] private InputSystem_Alpha _input;
    [SerializeField] private UIManager _ui;
    [SerializeField] private CameraCore _camera;
    [SerializeField] private InventoryPageFlow _inventoryPageFlow;
    [SerializeField] private InventoryView _inventoryView;

    private void Awake()
    {
        _player.Bind(_input, _ui, _camera);

        _inventoryPageFlow.HandlePageChanged += _inventoryView.HandlePageChanged;
        _inventoryPageFlow.OnInventoryActiveChanged += _player.InventoryActiveChanged;
    }

    private void Update()
    {
        if (_input.IsInventory)
        {
            if (_inventoryPageFlow.CurrentPage == EInventoryPage.Closed)
            {
                _inventoryPageFlow.OpenPage((int)EInventoryPage.Category);
                _camera.Cursour(true);
            }
            else
            {
                _inventoryPageFlow.ClosePage();
                _camera.Cursour(false);
            }
        }
        else if(_inventoryPageFlow.CurrentPage == EInventoryPage.Closed)
        {
            _camera.Cursour(false);
        }
    }
}
