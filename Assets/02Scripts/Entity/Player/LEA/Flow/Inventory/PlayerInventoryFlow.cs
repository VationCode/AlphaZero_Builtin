using UnityEngine;

public class PlayerInventoryFlow : MonoBehaviour
{
    private AlphaInputSystem _input;
    private CameraCore _camera;
    private PlayerCore _player;
    private InventoryPresenter _inventoryPresenter;

    public void Bind(PlayerCore p_core)
    {
        if (p_core == null) return;

        _player = p_core;
        _input = p_core.Input;
        _camera = p_core.CameraCore;

        TrySynchronizeInventoryState();
    }

    public void BindPresenter(InventoryPresenter p_inventoryPresenter)
    {
        if (_inventoryPresenter != null)
        {
            _inventoryPresenter.OpenStateChanged -= OnInventoryOpenStateChanged;
        }

        _inventoryPresenter = p_inventoryPresenter;

        if (_inventoryPresenter == null) return;

        _inventoryPresenter.OpenStateChanged += OnInventoryOpenStateChanged;

        TrySynchronizeInventoryState();
    }

    private void Update()
    {
        if (!_input.IsInventory || _inventoryPresenter == null)
            return;

        ToggleWindow();
    }
    private void TrySynchronizeInventoryState()
    {
        if (_player == null || _camera == null || _inventoryPresenter == null)
        {
            return;
        }

        OnInventoryOpenStateChanged(_inventoryPresenter.IsOpen);
    }
    private void ToggleWindow()
    {
        _inventoryPresenter.ToggleWindow();
    }

    private void OnInventoryOpenStateChanged(bool p_isOpen)
    {
        if (_player == null || _camera == null) return;
        _player.SetCombatBlocked(p_isOpen);
        _camera.Cursour(p_isOpen);
    }

    private void OnDestroy()
    {
        if (_inventoryPresenter != null)
        {
            _inventoryPresenter.OpenStateChanged -= OnInventoryOpenStateChanged;
        }
    }
}
