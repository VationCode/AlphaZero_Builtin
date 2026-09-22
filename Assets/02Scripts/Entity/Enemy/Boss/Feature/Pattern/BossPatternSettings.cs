using System;
using UnityEngine;

namespace Alpha.Boss
{
    // 공격 방식과 무관한 공통 선택 설정이다. 실행 상태는 저장하지 않는다.
    [Serializable]
    public sealed class BossPatternSettings
    {
        [SerializeField, Tooltip("패턴을 구분하는 ID입니다. 거리나 공격 타입을 의미하지 않습니다.")]
        private string _id = "Pattern";
        [SerializeField, Min(0f), Tooltip("이 패턴을 선택·유지할 최소 수평 거리입니다. 경계값을 포함합니다.")]
        private float _minDistance;
        [SerializeField, Min(0f), Tooltip("이 패턴을 선택·추적할 최대 수평 거리입니다. 접근 중 벗어나면 재선택합니다.")]
        private float _maxDistance = 20f;
        [SerializeField, Min(0f), Tooltip("선택한 패턴을 시작할 수평 거리입니다. 도착 시 0.05m 오차를 허용하여 정지·Prepare를 판단합니다. 선택 거리 범위는 넘지 않습니다.")]
        private float _attackStartDistance = 20f;
        [SerializeField, Min(0f), Tooltip("실행 시작부터 다음 실행까지의 쿨다운(초)입니다. 중단되어도 유지합니다.")]
        private float _cooldown = 1f;
        [SerializeField, Min(0f), Tooltip("실행 가능한 후보 사이의 선택 가중치입니다. 0이면 자동 선택에서 제외합니다.")]
        private float _selectionWeight = 1f;

        public string Id => _id;
        public float MinDistance => _minDistance;
        public float MaxDistance => _maxDistance;
        public float AttackStartDistance => _attackStartDistance;
        public float Cooldown => _cooldown;
        public float SelectionWeight => _selectionWeight;

        public BossPatternSettings() { }
        public BossPatternSettings(string p_id, float p_minDistance, float p_maxDistance,
            float p_cooldown = 0f, float p_selectionWeight = 1f, float? p_attackStartDistance = null)
        {
            if (!Finite(p_minDistance) || p_minDistance < 0f) throw new ArgumentOutOfRangeException(nameof(p_minDistance));
            if (!Finite(p_maxDistance) || p_maxDistance < p_minDistance) throw new ArgumentOutOfRangeException(nameof(p_maxDistance));
            if (!Finite(p_cooldown) || p_cooldown < 0f) throw new ArgumentOutOfRangeException(nameof(p_cooldown));
            if (!Finite(p_selectionWeight) || p_selectionWeight < 0f) throw new ArgumentOutOfRangeException(nameof(p_selectionWeight));
            float attackStartDistance = p_attackStartDistance ?? p_maxDistance;
            if (!Finite(attackStartDistance) || attackStartDistance < p_minDistance || attackStartDistance > p_maxDistance)
                throw new ArgumentOutOfRangeException(nameof(p_attackStartDistance));
            _id = p_id ?? string.Empty;
            _minDistance = p_minDistance; _maxDistance = p_maxDistance;
            _cooldown = p_cooldown; _selectionWeight = p_selectionWeight;
            _attackStartDistance = attackStartDistance;
        }

        public BossPatternSettings Copy() => new(_id, _minDistance, _maxDistance, _cooldown, _selectionWeight, _attackStartDistance);

        internal void Validate()
        {
            _id ??= string.Empty;
            _minDistance = NonNegative(_minDistance);
            _maxDistance = Mathf.Max(_minDistance, NonNegative(_maxDistance));
            _attackStartDistance = Mathf.Min(_maxDistance, Mathf.Max(_minDistance, NonNegative(_attackStartDistance)));
            _cooldown = NonNegative(_cooldown);
            _selectionWeight = NonNegative(_selectionWeight);
        }

        internal static float NonNegative(float p_value) => Finite(p_value) ? Mathf.Max(0f, p_value) : 0f;
        private static bool Finite(float p_value) => !float.IsNaN(p_value) && !float.IsInfinity(p_value);
    }
}
