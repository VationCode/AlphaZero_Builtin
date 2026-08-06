using UnityEngine;

namespace Alpha.Item
{
    // 월드에 배치된 아이템의 데이터 키와 남은 수량을 관리한다.
    public class PickupItemInfo : MonoBehaviour
    {
        [SerializeField]
        private int _itemId;
        [SerializeField]
        private int _count = 1;
        [SerializeField]
        private EItemType _itemType;

        public int ItemId => _itemId;
        public int Count => _count;
        public EItemType ItemType => _itemType;

        // 요청 수량만큼 소비하고 모두 소진되면 월드 오브젝트를 제거한다.
        public int Consume(int p_count)
        {
            if (p_count <= 0 || _count <= 0)
                return 0;

            // 보유 수량을 넘지 않도록 제한
            int consumedCount = Mathf.Min(p_count, _count);

            _count -= consumedCount;

            // 전부 습득했다면 월드에서 제거
            if (_count == 0)
                Destroy(gameObject);

            return consumedCount;
        }
    }
}
