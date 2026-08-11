using Alpha.Player.Slot;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Alpha.Player.Inventory
{
    // InventorySlot과 ItemSlotView의 생성·갱신을 연결한다.
    public class InventorySlotScrollView : MonoBehaviour
    {
        private ResourceLoadSystem _resourceLoader;

        [SerializeField] private ItemSlotView _slotViewPrefab;
        [SerializeField] private Transform _contentRoot;

        private readonly Dictionary<InventorySlot, ItemSlotView> _slotViewDict = new();
        private ScrollRect _scrollRect;
        private Coroutine _scrollRefreshRoutine;

        public event Action<InventorySlotScrollView> OnAddSlotRequested;

        // ScrollRect가 자신 또는 하위 계층에 있는 두 프리팹 구조를 모두 지원한다.
        private void Awake()
        {
            _scrollRect = GetComponentInChildren<ScrollRect>(true);
        }

        // 비활성화 중 변경된 레이아웃을 확정하고 시작 위치에서 연다.
        private void OnEnable()
        {
            RequestFirstSlotSnap();
        }

        // 슬롯 추가 버튼 요청을 상위 페이지에 알린다.
        public void RequestAddSlot()
        {
            OnAddSlotRequested?.Invoke(this);
        }

        // 도메인 슬롯에 대응하는 View를 생성하고 변경 이벤트를 연결한다.
        public ItemSlotView AddSlotView(InventorySlot p_slot, ResourceLoadSystem p_resourceLoader)
        {
            if (p_slot == null || _slotViewPrefab == null || _contentRoot == null)
            {
                return null;
            }

            // 이미 생성된 슬롯은 중복 생성하지 않고 기존 View를 반환한다.
            if (_slotViewDict.TryGetValue(p_slot, out ItemSlotView existingView))
            {
                return existingView;
            }

            if (p_resourceLoader != null)
            {
                _resourceLoader = p_resourceLoader;
            }

            // 새 View를 생성한 뒤 도메인 슬롯 변경을 구독한다.
            ItemSlotView slotView = Instantiate(_slotViewPrefab, _contentRoot);

            _slotViewDict.Add(p_slot, slotView);
            p_slot.OnChanged += HandleSlotChanged;

            // 입력 View에 소유 InventoryView와 고유 슬롯 인덱스를 전달한다.
            InventorySlotInteractionView interactionView = slotView.GetComponent<InventorySlotInteractionView>();

            if (interactionView != null)
            {
                InventoryView owner = GetComponentInParent<InventoryView>(true);

                interactionView.Bind(owner, p_slot.Index);
            }
            else
            {
                Debug.LogWarning($"{slotView.name}에 " + $"{nameof(InventorySlotInteractionView)}가 없습니다.", slotView);
            }

            ApplySlotView(p_slot, slotView);

            // 슬롯 추가 후에도 목록의 첫 번째 슬롯 위치를 유지한다.
            RequestFirstSlotSnap();
            return slotView;
        }

        // 첫 슬롯 스냅 요청을 하나로 합쳐 이전 보정과 새 보정이 충돌하지 않게 한다.
        private void RequestFirstSlotSnap()
        {
            if (!isActiveAndEnabled || _scrollRect == null)
                return;

            if (_scrollRefreshRoutine != null)
            {
                StopCoroutine(_scrollRefreshRoutine);
            }

            _scrollRefreshRoutine = StartCoroutine(SnapToFirstSlotAfterLayout());
        }

        // Content 크기가 확정된 다음 관성을 제거하고 첫 슬롯의 실제 시작 위치를 적용한다.
        private IEnumerator SnapToFirstSlotAfterLayout()
        {
            yield return null;

            if (_contentRoot is not RectTransform contentRect)
            {
                _scrollRefreshRoutine = null;
                yield break;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            Canvas.ForceUpdateCanvases();
            _scrollRect.StopMovement();

            Vector2 position = contentRect.anchoredPosition;

            if (_scrollRect.horizontal)
            {
                position.x = 0f;
            }

            if (_scrollRect.vertical)
            {
                position.y = 0f;
            }

            contentRect.anchoredPosition = position;
            _scrollRefreshRoutine = null;
        }

        // HandleSlotChanged 이벤트를 받아 필요한 후속 처리를 수행한다.
        private void HandleSlotChanged(InventorySlot p_slot)
        {
            if (p_slot == null || !_slotViewDict.TryGetValue(p_slot, out ItemSlotView slotView))
            {
                return;
            }

            ApplySlotView(p_slot, slotView);
        }

        // 도메인 슬롯을 공용 화면 데이터로 변환해 View에 적용한다.
        private void ApplySlotView(InventorySlot p_slot, ItemSlotView p_slotView)
        {
            if (p_slot == null || p_slotView == null)
                return;

            p_slotView.Apply(CreateViewData(p_slot), _resourceLoader);
        }

        // 빈 슬롯 여부를 포함한 ItemSlotViewData를 생성한다.
        private static ItemSlotViewData CreateViewData(InventorySlot p_slot)
        {
            if (p_slot.IsEmpty)
            {
                return new ItemSlotViewData(true, EItemType.None, string.Empty, string.Empty, 0);
            }

            ItemDTO item = p_slot.Item;

            return new ItemSlotViewData(false, item.ItemType, item.Name, item.IconKey, p_slot.Count);
        }

        // 비활성화 시 진행 중인 보정과 ScrollRect 관성을 함께 정리한다.
        private void OnDisable()
        {
            if (_scrollRefreshRoutine != null)
            {
                StopCoroutine(_scrollRefreshRoutine);
                _scrollRefreshRoutine = null;
            }

            _scrollRect?.StopMovement();
        }

        // 모든 슬롯 변경 구독과 재사용 View 상태를 정리한다.
        private void OnDestroy()
        {
            // 슬롯별 변경 이벤트를 해제하고 연결된 View를 빈 표시로 되돌린다.
            foreach (var pair in _slotViewDict)
            {
                InventorySlot slot = pair.Key;
                ItemSlotView slotView = pair.Value;

                if (slot != null)
                {
                    slot.OnChanged -= HandleSlotChanged;
                }

                slotView?.ResetView();
            }

            // 재사용할 수 없는 참조를 마지막에 비운다.
            _slotViewDict.Clear();
            _resourceLoader = null;
        }
    }
}
