using System;
using UnityEngine;

namespace Alpha.Boss
{
    // 애니메이션의 한 시점에 함께 발사할 위치 목록을 보관한다.
    [Serializable]
    public sealed class BossRangeFireGroup
    {
        [SerializeField, Min(0f)]
        [Tooltip("공격 애니메이션 시작 후 이 그룹을 발사할 시간(초)입니다.")]
        private float _fireTimeSeconds;

        [SerializeField]
        [Tooltip("유효한 항목마다 투사체 또는 지면파 하나를 생성합니다. 위치는 발사 순간에 읽으며, FirePositionForward에서는 각 발사점의 +Z 방향을 사용합니다.")]
        private Transform[] _spawnPoints = Array.Empty<Transform>();

        public float FireTimeSeconds => _fireTimeSeconds;
        public int SpawnPointCount => _spawnPoints?.Length ?? 0;

        public bool HasSpawnPoint
        {
            get
            {
                for (int index = 0; index < SpawnPointCount; index++)
                    if (_spawnPoints[index] != null)
                        return true;
                return false;
            }
        }

        public Transform GetSpawnPoint(int p_index)
        {
            return p_index >= 0 && p_index < SpawnPointCount ? _spawnPoints[p_index] : null;
        }

        // 실행 여부는 CombatContext가 소유하며 이 설정에는 기록하지 않는다.
        public void Validate()
        {
            _fireTimeSeconds = Mathf.Max(0f, _fireTimeSeconds);
            _spawnPoints ??= Array.Empty<Transform>();
        }
    }
}
