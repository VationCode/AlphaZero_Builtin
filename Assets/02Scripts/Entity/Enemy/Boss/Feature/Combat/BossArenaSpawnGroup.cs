using System;
using UnityEngine;

namespace Alpha.Boss
{
    // 한 시점에 생성할 지점들을 보관한다. 각 지점의 +Z가 해당 공격의 진행 방향이다.
    [Serializable]
    public sealed class BossArenaSpawnGroup
    {
        [SerializeField, Min(0f), Tooltip("공격 애니메이션 시작 후 생성 시간(초)입니다.")]
        private float _spawnTimeSeconds;
        [SerializeField, Tooltip("각 지점마다 하나씩 생성합니다. 지점의 위치와 +Z 방향은 생성 순간에 저장합니다.")]
        private Transform[] _spawnPoints = Array.Empty<Transform>();

        public float SpawnTimeSeconds => _spawnTimeSeconds;
        public int SpawnPointCount => _spawnPoints?.Length ?? 0;
        public Transform GetSpawnPoint(int p_index) =>
            p_index >= 0 && p_index < SpawnPointCount ? _spawnPoints[p_index] : null;

        public void Validate()
        {
            _spawnTimeSeconds = float.IsNaN(_spawnTimeSeconds) || float.IsInfinity(_spawnTimeSeconds)
                ? 0f : Mathf.Max(0f, _spawnTimeSeconds);
            _spawnPoints ??= Array.Empty<Transform>();
        }
    }
}
