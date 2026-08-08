using Alpha.Player.Slot;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Player.Equipment
{
    // WeaponSlotBinding 처리에 함께 사용되는 값들을 묶는다.
    [Serializable]
    public struct WeaponSlotBinding
    {
        [SerializeField] private EWeaponType _weaponType;
        [SerializeField] private EquipmentSlotView _view;

        public EWeaponType WeaponType => _weaponType;
        public EquipmentSlotView View => _view;
    }

    // ArmorSlotBinding 처리에 함께 사용되는 값들을 묶는다.
    [Serializable]
    public struct ArmorSlotBinding
    {
        [SerializeField] private EArmorType _armorType;
        [SerializeField] private EquipmentSlotView _view;

        public EArmorType ArmorType => _armorType;
        public EquipmentSlotView View => _view;
    }

    // Equipment UI를 표시하고 사용자 요청을 외부 구독자에게 알린다.
    public class EquipmentView : MonoBehaviour
    {
        [Header("Weapon Slots")]
        [SerializeField]
        private WeaponSlotBinding[] _weaponSlotBindings;

        [Header("Armor Slots")]
        [SerializeField]
        private ArmorSlotBinding[] _armorSlotBindings;

        private readonly Dictionary<EquipmentSlot, EquipmentSlotView>
            _slotViewDict = new();

        private EquipmentContext _context;
        private ResourceLoadSystem _resourceLoader;

        // 장착 요청을 외부 구독자에게 알린다.
        public event Action<int, EquipmentSlot> OnEquipRequested;

        // 장비 해제 요청을 외부 구독자에게 알린다.
        public event Action<EquipmentSlot, int?> OnUnequipRequested;

        // 장비 상태와 화면 슬롯을 연결하고 최초 표시를 갱신한다.
        public bool Bind(
            EquipmentContext p_context,
            ResourceLoadSystem p_resourceLoader)
        {
            if (p_context == null ||
                p_resourceLoader == null)
            {
                return false;
            }

            // 재연결 전에 이전 이벤트와 슬롯 상호작용을 정리한다.
            Unbind();

            _context = p_context;
            _resourceLoader = p_resourceLoader;

            // 도메인 슬롯과 Inspector에 지정된 View를 일대일로 연결한다.
            RegisterSlotViews();
            _context.OnSlotChanged += HandleSlotChanged;
            RefreshAll();

            return true;
        }

        // 장비 슬롯에 드롭된 인벤토리 아이템의 장착 요청을 발행한다.
        internal void RequestEquip(int p_inventorySlotIndex, EquipmentSlot p_targetSlot)
        {
            OnEquipRequested?.Invoke(p_inventorySlotIndex, p_targetSlot);
        }

        // 더블 클릭한 장비의 해제 요청을 발행한다.
        internal void RequestUnequip(EquipmentSlot p_slot)
        {
            OnUnequipRequested?.Invoke(p_slot, null);
        }

        // 무기·방어구 바인딩을 도메인 슬롯과 화면 슬롯으로 연결한다.
        private void RegisterSlotViews()
        {
            _slotViewDict.Clear();

            // 무기 타입별 도메인 슬롯을 찾아 대응 View를 등록한다.
            if (_weaponSlotBindings != null)
            {
                foreach (WeaponSlotBinding binding in _weaponSlotBindings)
                {
                    if (_context.TryGetWeaponSlot(binding.WeaponType, out WeaponEquipmentSlot slot))
                    {
                        Register(slot, binding.View);
                    }
                }
            }

            // 방어구 타입별 도메인 슬롯을 찾아 대응 View를 등록한다.
            if (_armorSlotBindings != null)
            {
                foreach (ArmorSlotBinding binding in _armorSlotBindings)
                {
                    if (_context.TryGetArmorSlot(binding.ArmorType, out ArmorEquipmentSlot slot))
                    {
                        Register(slot, binding.View);
                    }
                }
            }
        }

        // 하나의 도메인 슬롯과 View 및 상호작용 컴포넌트를 묶는다.
        private void Register(EquipmentSlot p_slot, EquipmentSlotView p_view)
        {
            if (p_slot == null || p_view == null)
                return;

            _slotViewDict[p_slot] = p_view;

            // 입력 View에도 같은 도메인 슬롯을 전달해 직접 요청할 수 있게 한다.
            EquipmentSlotInteractionView interactionView =
                p_view.GetComponent<EquipmentSlotInteractionView>();

            if (interactionView != null)
            {
                interactionView.Bind(this, p_slot);
            }
            else
            {
                Debug.LogWarning($"{p_view.name}에 " + $"{nameof(EquipmentSlotInteractionView)}가 없습니다.", p_view);
            }
        }

        // HandleSlotChanged 이벤트를 받아 필요한 후속 처리를 수행한다.
        private void HandleSlotChanged(EquipmentSlot p_slot)
        {
            if (!_slotViewDict.TryGetValue(p_slot, out EquipmentSlotView slotView))
            {
                return;
            }

            ApplySlotView(p_slot, slotView);
        }

        // RefreshAll 표시 또는 캐시를 최신 상태로 갱신한다.
        private void RefreshAll()
        {
            foreach (var pair in _slotViewDict)
            {
                ApplySlotView(pair.Key, pair.Value);
            }
        }

        // 장비 슬롯 상태를 공용 ItemSlotViewData로 변환해 표시한다.
        private void ApplySlotView(EquipmentSlot p_slot, EquipmentSlotView p_slotView)
        {
            ItemDTO item = p_slot.Item;

            // 빈 슬롯과 장착 슬롯을 동일한 View 입력 구조로 만든다.
            ItemSlotViewData viewData = item == null
                ? new ItemSlotViewData(true, EItemType.None, string.Empty, string.Empty, 0)
                : new ItemSlotViewData(false, item.ItemType, item.Name, item.IconKey, 1);

            p_slotView.Apply(viewData, _resourceLoader);
        }

        // 이벤트와 슬롯 상호작용의 이전 연결을 모두 해제한다.
        private void Unbind()
        {
            if (_context != null)
            {
                _context.OnSlotChanged -= HandleSlotChanged;
            }

            // 각 슬롯 입력 View가 이전 도메인 슬롯을 참조하지 않게 한다.
            foreach (EquipmentSlotView slotView
                     in _slotViewDict.Values)
            {
                slotView?.GetComponent<EquipmentSlotInteractionView>()?.Unbind();
            }

            _slotViewDict.Clear();
            _context = null;
            _resourceLoader = null;
        }

        // 객체 해제 시 등록한 이벤트와 참조를 정리한다.
        private void OnDestroy()
        {
            Unbind();
        }
    }
}
