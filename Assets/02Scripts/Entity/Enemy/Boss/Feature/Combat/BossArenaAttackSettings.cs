using System;
using UnityEngine;
using ArenaAttackEntity = Alpha.Boss.AOEAttack;

namespace Alpha.Boss
{
    // 지정 지점의 생성 시간과 이동 설정을 보관한다. 피해 범위는 공격 Prefab이 소유한다.
    [Serializable]
    public sealed class BossArenaAttackSettings
    {
        [SerializeField] private ArenaAttackEntity _attackPrefab;
        [SerializeField, Min(0.01f)] private float _moveSpeed = 10f;
        [SerializeField, Min(0.01f)] private float _maximumDistance = 50f;
        [SerializeField, Tooltip("그룹별 시간에 모든 SpawnPoint에서 동시에 생성합니다. 애니메이션이 끝나도 남은 그룹을 실행합니다.")]
        private BossArenaSpawnGroup[] _spawnGroups = Array.Empty<BossArenaSpawnGroup>();

        public ArenaAttackEntity AttackPrefab => _attackPrefab;
        public float MoveSpeed => _moveSpeed;
        public float MaximumDistance => _maximumDistance;
        public int SpawnGroupCount => _spawnGroups?.Length ?? 0;
        public BossArenaSpawnGroup GetSpawnGroup(int p_index) =>
            p_index >= 0 && p_index < SpawnGroupCount ? _spawnGroups[p_index] : null;

        public void Validate()
        {
            _moveSpeed = Positive(_moveSpeed, 10f);
            _maximumDistance = Positive(_maximumDistance, 50f);
            _spawnGroups ??= Array.Empty<BossArenaSpawnGroup>();
            for (int i = 0; i < _spawnGroups.Length; i++)
            {
                _spawnGroups[i] ??= new BossArenaSpawnGroup();
                _spawnGroups[i].Validate();
            }
        }

        private static float Positive(float p_value, float p_default) =>
            float.IsNaN(p_value) || float.IsInfinity(p_value) ? p_default : Mathf.Max(0.01f, p_value);
    }
}
