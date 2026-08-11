using Alpha.Item;
using Alpha.Player.Inventory;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alpha.Player
{
    // ItemPickupFlow 요청의 조건과 실행 순서를 결정한다.
    public class ItemPickupFlow : MonoBehaviour
    {
        private InventoryModule _inventoryModule;
        private ItemDatabaseManager _itemDatabase;
        private AlphaInputSystem _input;
        private readonly List<PickupItemInfo> _candidates = new();
        private bool _wasInteractionAvailable;

        public bool HasCandidate => TryGetCandidate(out _);
        public event Action<bool> OnInteractionAvailabilityChanged;

        // 아이템 추가 기능과 아이템 원본 데이터 조회 수단을 연결한다.
        public void Bind(
            InventoryModule p_inventoryModule,
            ItemDatabaseManager p_itemDatabase,
            AlphaInputSystem p_input)
        {
            _inventoryModule = p_inventoryModule;
            _itemDatabase = p_itemDatabase;
            _input = p_input;
        }

        // Pickup 아이템을 인벤토리에 추가하고 결과를 반영한다.
        public bool Pickup(PickupItemInfo p_pickup)
        {
            if (p_pickup == null ||
                p_pickup.Count <= 0 ||
                _inventoryModule == null ||
                _itemDatabase == null)
            {
                return false;
            }

            if (!_itemDatabase.TryGetItem(p_pickup.ItemType, p_pickup.ItemId, out ItemDTO item))
                return false;
            

            int addedCount = _inventoryModule.AddItem(item, p_pickup.Count);

            if (addedCount <= 0)
                return false;

            // 인벤토리에 실제 추가된 수량만 월드 아이템에서 차감한다.
            p_pickup.Consume(addedCount);

            return true;
        }

        // 현재 범위에서 가장 가까운 아이템의 픽업을 시도한다.
        public bool TryPickupCandidate()
        {
            if (!TryGetCandidate(out PickupItemInfo pickup))
                return false;

            bool succeeded = Pickup(pickup);

            if (pickup == null || pickup.Count <= 0)
                _candidates.Remove(pickup);

            RefreshInteractionAvailability();

            return succeeded;
        }

        // 진입한 아이템은 즉시 습득하지 않고 F키 승인을 기다리는 후보로 등록한다.
        private void OnTriggerEnter(Collider p_other)
        {
            PickupItemInfo pickup = p_other.GetComponentInParent<PickupItemInfo>();

            if (pickup == null || _candidates.Contains(pickup))
                return;

            _candidates.Add(pickup);
            RefreshInteractionAvailability();
        }

        private void OnTriggerExit(Collider p_other)
        {
            PickupItemInfo pickup = p_other.GetComponentInParent<PickupItemInfo>();

            if (pickup == null || !_candidates.Remove(pickup))
                return;

            RefreshInteractionAvailability();
        }

        // Interaction 입력과 외부에서 제거된 아이템의 UI 상태를 함께 갱신한다.
        private void Update()
        {
            RefreshInteractionAvailability();

            if (_input != null &&
                _input.IsInteraction &&
                HasCandidate)
            {
                TryPickupCandidate();
            }
        }

        private bool TryGetCandidate(out PickupItemInfo p_candidate)
        {
            p_candidate = null;
            float closestSqrDistance = float.PositiveInfinity;

            for (int index = _candidates.Count - 1; index >= 0; index--)
            {
                PickupItemInfo candidate = _candidates[index];

                if (candidate == null || candidate.Count <= 0)
                {
                    _candidates.RemoveAt(index);
                    continue;
                }

                float sqrDistance =
                    (candidate.transform.position - transform.position).sqrMagnitude;

                if (sqrDistance >= closestSqrDistance)
                    continue;

                closestSqrDistance = sqrDistance;
                p_candidate = candidate;
            }

            return p_candidate != null;
        }

        private void RefreshInteractionAvailability()
        {
            bool isAvailable = TryGetCandidate(out _);

            if (_wasInteractionAvailable == isAvailable)
                return;

            _wasInteractionAvailable = isAvailable;
            OnInteractionAvailabilityChanged?.Invoke(isAvailable);
        }
    }
}

