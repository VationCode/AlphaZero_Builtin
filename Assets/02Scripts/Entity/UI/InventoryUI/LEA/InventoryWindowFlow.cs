using System;
using UnityEngine;

namespace Alpha.Inventory
{
    // 입력에 따른 창 활성화 판단

    public class InventoryWindowFlow : MonoBehaviour
    {
        private AlphaInputSystem _input;

        public bool IsOpen { get; private set; }

        public event Action<bool> OnWindowActivate;

        public void Bind(AlphaInputSystem p_input)
        {
            _input = p_input;
        }

        private void Update()
        {
            if (_input != null && _input.IsInventory)
                SetWindowActive(!IsOpen);
        }

        // 인벤토리 창
        public void SetWindowActive(bool p_isActive)
        {
            if (IsOpen == p_isActive)
                return;

            IsOpen = p_isActive;
            OnWindowActivate?.Invoke(IsOpen);
        }
    }
}