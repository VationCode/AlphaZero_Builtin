using Alpha.Player.Equipment;
using System;
using UnityEngine;

namespace Alpha.Player.Inventory
{
    // Inventory UI의 대표 진입점이며 사용자 요청을 Flow로 전달한다.
    public class InventoryView : MonoBehaviour
    {
        private InventoryContext _context;
        private ResourceLoadSystem _resourceLoader;
        private InventoryFlow _inventoryFlow;
        private EquipmentFlow _equipmentFlow;

        [SerializeField] private GameObject _inventoryRoot;

        [Header("Active Views")]
        [SerializeField]
        private InventoryActiveView[] _inventoryActiveViews;

        private InventoryActiveView _category;

        [Header("Item Group Views")]
        [SerializeField]
        private InventoryItemPageView[] _itemPageViews;

        // 하위 화면을 수집해 카테고리와 아이템 페이지 목록으로 분류한다.
        private void Awake()
        {
            InventoryActiveView[] inventoryViews = GetComponentsInChildren<InventoryActiveView>(true);

            // 첫 ActiveView는 카테고리 화면으로 사용한다.
            if (inventoryViews.Length > 0)
            {
                _category = inventoryViews[0];
            }

            _inventoryActiveViews = new InventoryActiveView[Mathf.Max(0, inventoryViews.Length - 1)];

            // 나머지는 아이템 종류별 활성 화면으로 보관한다.
            for (int i = 1; i < inventoryViews.Length; i++)
            {
                _inventoryActiveViews[i - 1] = inventoryViews[i];
            }

            _itemPageViews = GetComponentsInChildren<InventoryItemPageView>(true);
        }

        // 인벤토리 상태·Flow와 각 아이템 페이지를 연결한다.
        public bool Bind(InventoryContext p_context, ResourceLoadSystem p_resourceLoader,
                         InventoryFlow p_inventoryFlow, EquipmentFlow p_equipmentFlow)
        {
            if (p_context == null || p_resourceLoader == null ||
                p_inventoryFlow == null || p_equipmentFlow == null)
            {
                return false;
            }

            // 재연결 시 이전 Context 이벤트가 남지 않도록 해제한다.
            if (_context != null)
            {
                _context.OnSlotAdded -= HandleSlotAdded;
            }

            _context = p_context;
            _resourceLoader = p_resourceLoader;
            _inventoryFlow = p_inventoryFlow;
            _equipmentFlow = p_equipmentFlow;

            _context.OnSlotAdded += HandleSlotAdded;

            // 페이지별 슬롯 목록과 슬롯 추가 요청을 연결한다.
            foreach (InventoryItemPageView itemPageView in _itemPageViews)
            {
                if (itemPageView == null)
                    continue;

                itemPageView.OnAddSlotRequested -= HandleAddSlotRequested;
                itemPageView.OnAddSlotRequested += HandleAddSlotRequested;

                if (_context.TryGetSlotList(itemPageView.ItemType, out var slotList))
                {
                    itemPageView.Bind(slotList, _resourceLoader);
                }
            }

            return true;
        }

        // 인벤토리 열림 여부와 현재 페이지에 맞는 화면만 활성화한다.
        public void ApplyViewState(bool p_isInventoryOpen, EItemType p_pageType)
        {
            bool showCategory = p_isInventoryOpen && p_pageType == EItemType.None;

            _category?.ApplyActive(showCategory);

            // 선택된 아이템 종류와 일치하는 페이지 하나만 표시한다.
            foreach (InventoryActiveView activeView in _inventoryActiveViews)
            {
                if (activeView == null)
                    continue;

                bool showItemWindow = p_isInventoryOpen && activeView.ItemType == p_pageType;

                activeView.ApplyActive(showItemWindow);
            }

            _inventoryRoot?.SetActive(p_isInventoryOpen);
        }

        // UI 버튼 값을 아이템 타입으로 변환해 페이지 전환을 요청한다.
        public void RequestPage(int p_itemTypeValue)
        {
            if (!Enum.IsDefined(typeof(EItemType), p_itemTypeValue))
            {
                return;
            }

            _inventoryFlow.RequestOpenPage((EItemType)p_itemTypeValue);
        }

        // 인벤토리 닫기를 Flow에 요청한다.
        public void RequestCloseInventory()
        {
            _inventoryFlow.RequestCloseInventory();
        }

        // 더블 클릭한 인벤토리 아이템의 장착을 Flow에 요청한다.
        internal EEquipmentChangeResult RequestEquip(int p_inventorySlotIndex)
        {
            return _equipmentFlow.RequestEquip(p_inventorySlotIndex);
        }

        // 두 인벤토리 슬롯 사이의 이동을 Flow에 요청한다.
        internal void RequestTransfer(int p_sourceSlotIndex, int p_targetSlotIndex)
        {
            _inventoryFlow.RequestTransferItem(p_sourceSlotIndex, p_targetSlotIndex);
        }

        // 장비 아이템을 지정 인벤토리 슬롯으로 해제하도록 요청한다.
        internal EEquipmentChangeResult RequestUnequip(EquipmentSlot p_equipmentSlot, int p_targetInventorySlotIndex)
        {
            return _equipmentFlow.RequestUnequip(p_equipmentSlot, p_targetInventorySlotIndex);
        }

        // HandleAddSlotRequested 이벤트를 받아 필요한 후속 처리를 수행한다.
        private void HandleAddSlotRequested(EItemType p_itemType, int p_groupIndex)
        {
            _inventoryFlow.RequestAddSlot(p_itemType, p_groupIndex);
        }

        // 새 슬롯을 같은 아이템 타입의 페이지에 즉시 추가한다.
        private void HandleSlotAdded(EItemType p_itemType, InventorySlot p_slot)
        {
            // 슬롯 타입과 일치하는 페이지 하나를 찾아 View를 생성한다.
            foreach (InventoryItemPageView itemPageView in _itemPageViews)
            {
                if (itemPageView == null || itemPageView.ItemType != p_itemType)
                {
                    continue;
                }

                itemPageView.AddSlot(p_slot, _resourceLoader);
                return;
            }
        }

        // Context와 각 페이지에 등록한 이벤트를 모두 해제한다.
        private void OnDestroy()
        {
            // Context가 살아 있으면 동적 슬롯 추가 통지를 먼저 해제한다.
            if (_context != null)
            {
                _context.OnSlotAdded -= HandleSlotAdded;
            }

            if (_itemPageViews == null)
                return;

            // 각 페이지에서 전달하던 사용자 슬롯 추가 요청도 모두 해제한다.
            foreach (InventoryItemPageView itemPageView in _itemPageViews)
            {
                if (itemPageView != null)
                {
                    itemPageView.OnAddSlotRequested -= HandleAddSlotRequested;
                }
            }
        }
    }
}
