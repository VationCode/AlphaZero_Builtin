using System;
using UnityEngine;
using ArenaAttackEntity = Alpha.Boss.AOEAttack;

namespace Alpha.Boss
{
    // 타겟 주변 생성 범위, 회차별 개수, 반복 시간과 개별 공격 수명을 보관한다.
    [Serializable]
    public sealed class BossAreaAttackSettings
    {
        [SerializeField] private ArenaAttackEntity _attackPrefab;
        [SerializeField, Min(0f), Tooltip("매 회차 생성 순간의 타겟을 중심으로 위치를 선정할 XZ 반경입니다. 피해 범위는 Prefab의 Damage Collider가 담당합니다.")]
        private float _spawnRadius = 5f;
        [SerializeField, Min(1), Tooltip("매 회차에 동시에 생성할 최소 개수입니다. 회차마다 Min과 Max를 포함하여 새로 선정합니다.")]
        private int _minSpawnCount = 3;
        [SerializeField, Min(1)] private int _maxSpawnCount = 5;
        [SerializeField, Min(0f), Tooltip("공격 애니메이션 시작 후 첫 생성까지의 시간(초)입니다.")]
        private float _startTimeSeconds = 0.5f;
        [SerializeField, Min(0f), Tooltip("첫 생성부터 마지막 생성까지의 시간(초)입니다. Repeat Count가 1이면 첫 생성만 실행하며, 0초이면 모든 회차를 동시에 실행합니다.")]
        private float _durationSeconds = 4f;
        [SerializeField, Min(1), Tooltip("첫 생성을 포함한 총 생성 횟수입니다. Duration 동안 균등한 간격으로 실행합니다.")]
        private int _repeatCount = 3;
        [SerializeField, Min(0.01f), Tooltip("각 공격이 생성된 순간부터 제거될 때까지의 시간(초)입니다.")]
        private float _lifetime = 4f;
        [SerializeField, Tooltip("타겟 아래 지면을 찾을 레이어입니다. Trigger는 무시합니다.")]
        private LayerMask _groundMask = 1;
        [SerializeField, Min(0.01f)] private float _groundProbeHeight = 10f;
        [SerializeField, Min(0.01f)] private float _groundProbeDistance = 100f;

        public ArenaAttackEntity AttackPrefab => _attackPrefab;
        public float SpawnRadius => _spawnRadius;
        public int MinSpawnCount => _minSpawnCount;
        public int MaxSpawnCount => _maxSpawnCount;
        public float StartTimeSeconds => _startTimeSeconds;
        public float DurationSeconds => _durationSeconds;
        public int RepeatCount => _repeatCount;
        public float Lifetime => _lifetime;
        public LayerMask GroundMask => _groundMask;
        public float GroundProbeHeight => _groundProbeHeight;
        public float GroundProbeDistance => _groundProbeDistance;

        // 마지막 회차는 정확히 Start Time + Duration에 배치한다. 1회 패턴은 Duration을 기다리지 않는다.
        public float GetSpawnTimeSeconds(int p_index) =>
            _startTimeSeconds + (_repeatCount > 1 ? _durationSeconds * ((float)p_index / (_repeatCount - 1)) : 0f);

        public void Validate()
        {
            _spawnRadius = float.IsNaN(_spawnRadius) || float.IsInfinity(_spawnRadius)
                ? 5f : Mathf.Max(0f, _spawnRadius);
            _minSpawnCount = Mathf.Max(1, _minSpawnCount);
            _maxSpawnCount = Mathf.Max(_minSpawnCount, _maxSpawnCount);
            _startTimeSeconds = float.IsNaN(_startTimeSeconds) || float.IsInfinity(_startTimeSeconds)
                ? 0.5f : Mathf.Max(0f, _startTimeSeconds);
            _durationSeconds = float.IsNaN(_durationSeconds) || float.IsInfinity(_durationSeconds)
                ? 4f : Mathf.Max(0f, _durationSeconds);
            _repeatCount = Mathf.Max(1, _repeatCount);
            _lifetime = Positive(_lifetime, 4f);
            _groundProbeHeight = Positive(_groundProbeHeight, 10f);
            _groundProbeDistance = Mathf.Max(_groundProbeHeight, Positive(_groundProbeDistance, 100f));
        }

        private static float Positive(float p_value, float p_default) =>
            float.IsNaN(p_value) || float.IsInfinity(p_value) ? p_default : Mathf.Max(0.01f, p_value);
    }
}
