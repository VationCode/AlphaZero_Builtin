using Alpha.Player.Inventory;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Player.Inventory
{
    // 전달받은 화면 데이터만 표현한다.
    public class InventorySlotScrollView : MonoBehaviour
    {
        private ResourceLoadSystem _resourceLoader;

        [SerializeField]
        private InventorySlotView _slotViewPrefab;

        [SerializeField]
        private Transform _contentRoot;

        private readonly Dictionary<InventorySlot, InventorySlotView> _slotViewDict = new();

        public event Action<InventorySlotScrollView> OnAddSlotRequested;    // 슬롯 추가 요청 이벤트
        public event Action<int, int> OnTransferRequested;                  // Source SlotIndex, Target SlotIndex
        // 버튼에 의한 슬롯 추가 (View -> 로직으로의 요청)
        public void RequestAddSlot()
        {
            OnAddSlotRequested?.Invoke(this);
        }

        // ScrollView Content에 SlotView를 하나 생성한다.
        public InventorySlotView AddSlotView(InventorySlot p_slot, ResourceLoadSystem p_resourceLoader)
        {
            if (p_slot == null || _slotViewPrefab == null || _contentRoot == null)
            {
                return null;
            }

            // 동일 슬롯의 View 중복 생성을 방지한다.
            if (_slotViewDict.TryGetValue(p_slot, out InventorySlotView existingView))
            {
                return existingView;
            }

            if (p_resourceLoader != null)
                _resourceLoader = p_resourceLoader;

            // 슬롯UI 생성
            InventorySlotView slotView = Instantiate(_slotViewPrefab, _contentRoot);

            _slotViewDict.Add(p_slot, slotView);

            // ScrollView가 Domain 변경을 구독한다.
            p_slot.OnChanged += HandleSlotChanged;

            // Drag & Drop & Scroll 이벤트 관련 
            InventorySlotInteractionView interactionView =slotView.GetComponent<InventorySlotInteractionView>();
            if (interactionView != null)
            {
                interactionView.OnTransferRequested += HandleTransferRequested;
            }
            else
            {
                Debug.LogWarning($"{slotView.name}에 " + $"{nameof(interactionView)}가 없습니다.", slotView);
            }

            ApplySlotView(p_slot, slotView);

            return slotView;
        }

        private void HandleSlotChanged(InventorySlot p_slot)
        {
            if (p_slot == null)
                return;

            if (!_slotViewDict.TryGetValue(p_slot, out InventorySlotView slotView))
            {
                return;
            }

            ApplySlotView(p_slot, slotView);
        }

        // 하위 View의 요청을 상위 View로 전달
        private void HandleTransferRequested(int p_sourceSlotIndex, int p_targetSlotIndex)
        {
            OnTransferRequested?.Invoke(p_sourceSlotIndex, p_targetSlotIndex);
        }

        private void ApplySlotView(InventorySlot p_slot, InventorySlotView p_slotView)
        {
            if (p_slot == null || p_slotView == null)
                return;

            InventorySlotViewData viewData = CreateViewData(p_slot);

            p_slotView.Apply(viewData, _resourceLoader);
        }

        private InventorySlotViewData CreateViewData(InventorySlot p_slot)
        {
            if (p_slot.IsEmpty)
            {
                return new InventorySlotViewData(p_slot.Index, true, EItemType.None, 
                                                 string.Empty, string.Empty, 0);
            }

            ItemDTO item = p_slot.Item;

            return new InventorySlotViewData(p_slot.Index, false, item.ItemType, 
                                             item.Name, item.IconKey, p_slot.Count);
        }


        private void OnDestroy()
        {
            foreach (var pair in _slotViewDict)
            {
                InventorySlot slot = pair.Key;
                InventorySlotView slotView = pair.Value;

                if (slot != null)
                {
                    slot.OnChanged -= HandleSlotChanged;
                }

                if (slotView == null)
                    continue;

                InventorySlotInteractionView interactionView =slotView.GetComponent<InventorySlotInteractionView>();

                if (interactionView != null)
                {
                    interactionView.OnTransferRequested -= HandleTransferRequested;
                }

                slotView.ResetView();
            }

            _slotViewDict.Clear();
            _resourceLoader = null;
        }
    }
}

/*
InventorySlotDragView
  └─ Source 인덱스 제공

InventorySlotDropView
  └─ OnTransferRequested 발생

InventorySlotScrollView
  └─ DropView 이벤트 구독 후 상위로 전달
*/