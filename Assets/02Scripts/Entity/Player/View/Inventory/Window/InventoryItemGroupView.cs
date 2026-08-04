using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Player.Inventory
{
    // SlotScrollView 배열 관리
    // 슬롯 View 생성, 슬롯 추가 요청
    public class InventoryItemGroupView : MonoBehaviour
    {
        [Header("Window Type")]
        [SerializeField] private EItemType _itemType;

        [Header("Slot Group Views")]
        [SerializeField] private InventorySlotScrollView[] _slotScrollerViews;

        public EItemType ItemType => _itemType;

        public event Action<EItemType, int> OnAddSlotRequested;
        public event Action OnBackRequested;
        public void Bind(IReadOnlyList<InventorySlot> p_slotList, ResourceLoadSystem p_resourceLoader)
        {
            // 각 ScrollView의 AddBtn 요청 이벤트를 연결한다.
            BindScrollViewEvents();

            if (p_slotList == null)
                return;

            foreach (InventorySlot slot in p_slotList)
            {
                AddSlot(slot, p_resourceLoader);
            }
        }

        // 현재 각 AddBtn의 OnClick에 SlotScrollView의 RequestAddSlot()를 연결한상태이고
        // RequestAddSlot에는 OnAddSlotRequested가 동작하도록 되어있다.
        // 이에 버튼 클릭 시 로직에 AddSlot을 요청하고 로직에 있는 View 생성 이벤트가 발생하도록 한다.
        private void BindScrollViewEvents()
        {
            foreach (InventorySlotScrollView scrollView in _slotScrollerViews)
            {
                if (scrollView == null)
                    continue;

                scrollView.OnAddSlotRequested -= HandleAddSlotRequested;
                scrollView.OnAddSlotRequested += HandleAddSlotRequested;
            }
        }

        private void HandleAddSlotRequested(InventorySlotScrollView p_scrollView)
        {
            int groupIndex = Array.IndexOf(_slotScrollerViews, p_scrollView);

            if (groupIndex < 0)
                return;

            OnAddSlotRequested?.Invoke(_itemType, groupIndex);
        }

        // 각 ScrollView에 SlotView를 생성한다.
        public void AddSlot(InventorySlot p_slot, ResourceLoadSystem p_resourceLoader)
        {
            int groupIndex = GetGroupIndex(p_slot);

            if (groupIndex < 0 || groupIndex >= _slotScrollerViews.Length)
            {
                return;
            }

            _slotScrollerViews[groupIndex]?.AddSlotView(p_slot, p_resourceLoader);
        }

        // 논리 Slot의 세부 타입을 ScrollView 배열 인덱스로 변환한다
        private static int GetGroupIndex(InventorySlot p_slot)
        {
            switch (p_slot)
            {
                case WeaponInventorySlot weaponSlot:
                    return (int)weaponSlot.WeaponType;  // ItemTpye -> WeaponType -> Melee = 0, Ranged = 1, Special = 2

                case ArmorInventorySlot armorSlot:
                    return (int)armorSlot.ArmorType;

                case CommonInventorySlot:               // CommonInventorySlot은 단일 그룹(창마다 ScrollView가 하나이므로)으로 처리한다.
                    return 0;

                default:
                    return -1;
            }
        }
    }
}
