using System;
using UnityEngine;

namespace Alpha.Boss
{
    // 실제 판정 범위이며 패턴을 선택하는 타겟 거리와는 별개다.
    [Serializable]
    public sealed class BossMeleeSettings
    {
        [SerializeField, Min(0f), Tooltip("근접 공격 Module에 전달할 타격 반경입니다.")]
        private float _hitRadius = 2f;
        public float HitRadius => _hitRadius;
        internal void Validate() => _hitRadius = BossPatternSettings.NonNegative(_hitRadius);
    }

    [Serializable]
    public sealed class BossRangeSettings
    {
        [SerializeField, Min(1), Tooltip("공격 실행 시 함께 발사할 개수입니다.")]
        private int _projectileCount = 1;
        [SerializeField, Min(0f), Tooltip("발사 방향을 펼칠 전체 각도입니다. 0이면 같은 방향입니다.")]
        private float _spreadAngle;
        public int ProjectileCount => _projectileCount;
        public float SpreadAngle => _spreadAngle;
        internal void Validate()
        {
            _projectileCount = Mathf.Max(1, _projectileCount);
            _spreadAngle = Mathf.Min(360f, BossPatternSettings.NonNegative(_spreadAngle));
        }
    }

    [Serializable]
    public sealed class BossGlobalAoESettings
    {
        [SerializeField, Min(0f), Tooltip("광역 공격 Module에 전달할 생성 영역의 반경입니다.")]
        private float _areaRadius = 10f;
        [SerializeField, Min(1), Tooltip("공격 실행 시 생성할 광역 공격 개수입니다.")]
        private int _spawnCount = 1;
        public float AreaRadius => _areaRadius;
        public int SpawnCount => _spawnCount;
        internal void Validate()
        {
            _areaRadius = BossPatternSettings.NonNegative(_areaRadius);
            _spawnCount = Mathf.Max(1, _spawnCount);
        }
    }
}
