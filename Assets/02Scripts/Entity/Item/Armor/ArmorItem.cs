using UnityEngine;

namespace Alpha.Item.Armor
{
    // Armor Prefab의 런타임 데이터와 Inspector 전투 수치를 소유한다.
    public sealed class ArmorItem : MonoBehaviour
    {
        [Header("Defense Tuning")]
        [SerializeField, Min(0)]
        private int _baseDefense;

        public ArmorDTO Data { get; private set; }
        public int BaseDefense => _baseDefense;
        public bool IsInitialized => Data != null;

        public bool TryInitialize(ArmorDTO p_data)
        {
            if (p_data == null)
                return false;

            if (IsInitialized)
                return ReferenceEquals(Data, p_data);

            Data = p_data;
            return true;
        }
    }
}
