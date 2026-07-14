using UnityEngine;

public class PlayerInventoryFlow : MonoBehaviour
{
    private AlphaInputSystem _input;
    private CameraCore _camera;
    private PlayerCore _player;
    private InventoryPresenter _inventoryPresenter;

    public void Bind(PlayerCore p_core)
    {
        _player = p_core;
        _input = p_core.InputManager;
        _camera = p_core.CameraCore;
    }

    public void BindPresenter(InventoryPresenter p_inventoryPresenter)
    {
        _inventoryPresenter = p_inventoryPresenter;
    }

    private void Update()
    {
        if (!_input.IsInventory || _inventoryPresenter == null)
            return;

        ToggleWindow();
    }

    private void ToggleWindow()
    {
        bool isOpen = _inventoryPresenter.ToggleWindow();

        _player.InventoryActiveChanged(isOpen);
        _camera.Cursour(isOpen);
    }
}
