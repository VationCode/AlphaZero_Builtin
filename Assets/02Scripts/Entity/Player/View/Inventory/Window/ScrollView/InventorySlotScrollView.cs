using System.Collections.Generic;
using UnityEngine;
using System;

namespace Alpha.Player.Inventory
{
    public class InventorySlotScrollView : MonoBehaviour
    {
        [SerializeField]
        private InventorySlotView _slotViewPrefab;

        [SerializeField]
        private Transform _contentRoot;

        private readonly List<InventorySlotView> _slotViewList = new();

        public event Action<InventorySlotScrollView> OnAddSlotRequested;    // 슬롯 추가 요청 이벤트

        // 버튼에 의한 슬롯 추가
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

            InventorySlotView slotView = Instantiate(_slotViewPrefab, _contentRoot);

            slotView.Bind(p_slot, p_resourceLoader);
            
            _slotViewList.Add(slotView);

            return slotView;
        }

        private void OnDestroy()
        {
            foreach (InventorySlotView slotView in _slotViewList)
            {
                if (slotView != null)
                    slotView.Unbind();
            }

            _slotViewList.Clear();
        }
    }
}
