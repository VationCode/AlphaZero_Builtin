using System;
using Alpha.Inventory;
using UnityEngine;

namespace Alpha.Equipment
{
    /// <summary>
    /// Equipment 내부 Module, Presenter, View를 조립하고
    /// 외부에 Equipment의 단일 진입점을 제공한다.
    /// </summary>
    public class EquipmentCore : MonoBehaviour
    {
        [SerializeField] private EquipmentModule _module;
        [SerializeField] private EquipmentView _view;

        private EquipmentPresenter _presenter;

        public bool IsInitialized { get; private set; }

        // 장착된 무기가 변경됐을 때 Player에 전달한다.
        public event Action<EWeaponType, WeaponDTO> OnEquippedWeaponChanged;

        // 장착된 방어구가 변경됐을 때 Player에 전달한다.
        public event Action<EArmorType, ArmorDTO> OnEquippedArmorChanged;

        public bool Bind(ResourceLoadSystem p_resourceLoader, InventoryPresenter p_inventoryPresenter)
        {
            if (IsInitialized) 
                return true;

            if (_module == null || _view == null ||
                p_resourceLoader == null || p_inventoryPresenter == null)
            {
                Debug.LogError($"{nameof(EquipmentCore)}의 참조가 설정되지 않았습니다.", this);
                return false;
            }

            // Equipment 상태를 먼저 생성한다.
            if (!_module.Initialize())
                return false;

            BindModuleEvents();

            // Equipment 상태와 UI 표현을 연결한다.
            _presenter = new EquipmentPresenter(_module, _view, p_resourceLoader);

            _presenter.Initialize();

            // Inventory와 Equipment 사이의 Drag & Drop을 연결한다.
            _presenter.BindInventory(p_inventoryPresenter);

            IsInitialized = true;
            return true;
        }

        private void BindModuleEvents()
        {
            // 중복 연결을 방지한 뒤 Core 외부 이벤트로 전달한다.
            _module.OnEquippedWeaponChanged -= HandleEquippedWeaponChanged;
            _module.OnEquippedWeaponChanged += HandleEquippedWeaponChanged;

            _module.OnEquippedArmorChanged -= HandleEquippedArmorChanged;
            _module.OnEquippedArmorChanged += HandleEquippedArmorChanged;
        }

        #region ============================== Equipment Entry Point
        public bool TryGetEquippedWeapon(EWeaponType p_type, out WeaponDTO p_weapon)
        {
            p_weapon = null;

            return IsInitialized && _module.TryGetEquippedWeapon(p_type, out p_weapon);
        }
        #endregion ============================== /Equipment Entry Point

        #region ============================== Module Event
        private void HandleEquippedWeaponChanged(EWeaponType p_type, WeaponDTO p_weapon)
        {
            OnEquippedWeaponChanged?.Invoke(p_type, p_weapon);
        }

        private void HandleEquippedArmorChanged(EArmorType p_type, ArmorDTO p_armor)
        {
            OnEquippedArmorChanged?.Invoke(p_type, p_armor);
        }
        #endregion ============================== /Module Event

        private void OnDestroy()
        {
            // Inventory와 Equipment View에 연결된 Presenter 이벤트를 먼저 해제한다.
            _presenter?.Unbind();
            _presenter = null;

            if (_module != null)
            {
                _module.OnEquippedWeaponChanged -= HandleEquippedWeaponChanged;
                _module.OnEquippedArmorChanged -= HandleEquippedArmorChanged;
            }

            OnEquippedWeaponChanged = null;
            OnEquippedArmorChanged = null;
        }
    }
}
