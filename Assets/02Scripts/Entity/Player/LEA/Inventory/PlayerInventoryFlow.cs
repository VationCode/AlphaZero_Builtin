using Alpha.Inventory;
using Alpha.Mouse;
using UnityEngine;

namespace Alpha.Player
{
    public class PlayerInventoryFlow : MonoBehaviour
    {
        private AlphaInputSystem _input;
        private PlayerCore _player;
        private InventoryPresenter _inventoryPresenter;
        private MouseSystem _mouseSystem;
        public void Bind(PlayerCore p_core, InventoryPresenter p_inventoryPresenter)
        {
            if (p_core == null) return;

            _player = p_core;
            _input = p_core.Input;
            _inventoryPresenter = p_inventoryPresenter;
            _mouseSystem = p_core.MouseSystem;

            _inventoryPresenter.OpenStateChanged += OnInventoryOpenStateChanged;

            OnInventoryOpenStateChanged(_inventoryPresenter.IsOpen);
        }

        private void Update()
        {
            if (_input.IsInventory)
            {
                _inventoryPresenter.ToggleWindow();
            }
        }

        // Inventory 상태에 따라 전투 입력을 차단한다.
        private void OnInventoryOpenStateChanged(bool p_isOpen)
        {
            _player.SetCombatBlocked(p_isOpen);
            _mouseSystem.SetUICursor(p_isOpen);
        }

        private void OnDestroy()
        {
            if (_inventoryPresenter == null) return;

            _inventoryPresenter.OpenStateChanged -= OnInventoryOpenStateChanged;
        }
    }
}