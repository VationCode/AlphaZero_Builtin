using UnityEngine;

namespace Alpha.Inventory
{
    public class InventoryCore : MonoBehaviour
    {
        [SerializeField] private InventoryModule _module;
        [SerializeField] private InventoryWindowFlow _flow;
        [SerializeField] private InventoryView _view;

        public InventoryWindowFlow Flow => _flow;
        public InventoryPresenter Presenter => _presenter;
        private InventoryPresenter _presenter;
        public bool IsInitialized { get; private set; }

        public void Bind(AlphaInputSystem p_input, ResourceLoadSystem p_resourceLoader)
        {
            if (IsInitialized)
                return;

            if (_module == null || _flow == null || _view == null)
            {
                Debug.LogError("InventoryCore 내부 참조가 설정되지 않았습니다.");
                return;
            }

            _module.Initialize();
            _flow.Bind(p_input);

            // LEA와 View 연결
            _flow.OnWindowActivate += _view.SetWindowActive;
            _view.OnRequestWindowActive += _flow.SetWindowActive;

            _presenter = new InventoryPresenter(_module, _view, p_resourceLoader);

            _presenter.Initialize();

            IsInitialized = true;
        }

        public bool TryAddItem(ItemDTO p_item, int p_count, out int p_addedCount)
        {
            p_addedCount = 0;

            return IsInitialized && _module.TryAddItem(p_item, p_count, out p_addedCount);
        }
    }
}