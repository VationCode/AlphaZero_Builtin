using System;
using UnityEngine;

namespace Alpha.Inventory
{
    /// <summary>
    /// Inventory 내부 Module, Flow, View를 조립하고
    /// 외부에 Inventory의 단일 진입점을 제공한다.
    /// </summary>
    public class InventoryCore : MonoBehaviour
    {
        [SerializeField] private InventoryModule _module;
        [SerializeField] private InventoryView _view;

        public InventoryPresenter Presenter => _presenter;
        private InventoryPresenter _presenter;
        public bool IsInitialized { get; private set; }

        // Inventory 창 활성 상태를 외부 시스템에 전달
        public event Action<bool> OnWindowActiveChanged;
        public bool Bind(AlphaInputSystem p_input, ResourceLoadSystem p_resourceLoader)
        {
            if (IsInitialized) return true;

            if (_module == null || _view == null || p_input == null || p_resourceLoader == null)
                return false;
            

            if (!_module.Initialize())
                return false;

            // Inventory 입력과 화면 동작을 View에 연결한다.
            _view.Bind(p_input);

            _view.OnWindowActiveChanged -= HandleWindowActiveChanged;
            _view.OnWindowActiveChanged += HandleWindowActiveChanged;

            _presenter = new InventoryPresenter(_module, _view, p_resourceLoader);

            _presenter.Initialize();

            IsInitialized = true;
            return true;
        }

        public bool TryAddItem(ItemDTO p_item, int p_count, out int p_addedCount)
        {
            p_addedCount = 0;

            return IsInitialized && _module.TryAddItem(p_item, p_count, out p_addedCount);
        }

        #region ============================== 인벤토리 창
          /*InventoryView.OnWindowActiveChanged
                        ↓
            InventoryCore.HandleWindowActiveChanged()
                        ↓
            InventoryCore.OnWindowActiveChanged
                        ↓
            MouseSystem / PlayerCore*/
        private void HandleWindowActiveChanged(bool p_isActive)
        {
            OnWindowActiveChanged?.Invoke(p_isActive);
        }

        private void OnDestroy()
        {
            // Presenter가 Slot과 View에 연결한 이벤트를 먼저 해제한다.
            _presenter?.Unbind();
            _presenter = null;

            if (_view != null)
            {
                _view.OnWindowActiveChanged -= HandleWindowActiveChanged;
            }

            OnWindowActiveChanged = null;
        }
        #endregion ============================== /인벤토리 창
    }
}