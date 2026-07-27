using Alpha.Inventory;
using UnityEngine;

namespace Alpha.Equipment
{
    // Equipment 내부 객체를 조립하고 외부 진입점을 제공한다.
    public class EquipmentCore : MonoBehaviour
    {
        [SerializeField] private EquipmentModule _module;
        [SerializeField] private EquipmentView _view;

        private EquipmentPresenter _presenter;

        public EquipmentModule Module => _module;
        public bool IsInitialized { get; private set; }

        public void Bind(ResourceLoadSystem p_resourceLoader, InventoryPresenter p_inventoryPresenter)
        {
            if (IsInitialized)
                return;

            if (_module == null || _view == null || p_resourceLoader == null || p_inventoryPresenter == null)
            {
                Debug.LogError("EquipmentCore의 내부 참조 또는 외부 참조가 설정되지 않았습니다.");

                return;
            }

            // 상태를 먼저 생성한 뒤 View와 연결한다.
            _module.Initialize();

            _presenter = new EquipmentPresenter(_module, _view, p_resourceLoader);

            _presenter.Initialize();
            _presenter.BindInventory(p_inventoryPresenter);

            IsInitialized = true;
        }
    }
}
