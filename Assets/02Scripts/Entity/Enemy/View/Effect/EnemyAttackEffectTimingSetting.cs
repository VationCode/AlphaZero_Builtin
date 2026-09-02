using System;
using UnityEngine;

namespace Alpha.Enemy.Effect
{
    // 공격 애니메이션의 특정 시간 구간에 생성할 Effect 정보를 보관한다.
    [Serializable]
    public sealed class EnemyAttackEffectTimingSetting
    {
        [Tooltip("Start Time에 생성해 재생할 Effect Prefab입니다.")]
        [SerializeField]
        private GameObject _effectPrefab;

        [Tooltip("Effect를 생성할 위치입니다. 비어 있으면 EnemyAttackEffectView 위치를 사용합니다.")]
        [SerializeField]
        private Transform _spawnPoint;

        [Tooltip("활성 구간 동안 생성된 Effect가 Spawn Point를 따라갑니다.")]
        [SerializeField]
        private bool _followSpawnPoint = true;

        [Tooltip("공격 애니메이션 시작 후 Effect를 재생할 시간입니다.")]
        [SerializeField, Min(0f)]
        private float _startTimeSeconds;

        [Tooltip("Particle 방출을 멈출 시간입니다. Start Time보다 커야 합니다.")]
        [SerializeField, Min(0.01f)]
        private float _endTimeSeconds = 0.5f;

        [Tooltip("End Time 이후 남은 Particle이 사라질 때까지 Instance를 유지할 시간입니다.")]
        [SerializeField, Min(0f)]
        private float _tailDuration = 1f;

        public GameObject EffectPrefab => _effectPrefab;
        public Transform SpawnPoint => _spawnPoint;
        public bool FollowSpawnPoint => _followSpawnPoint;
        public float StartTimeSeconds => Mathf.Max(0f, _startTimeSeconds);
        public float EndTimeSeconds => Mathf.Max(
            StartTimeSeconds + 0.01f,
            _endTimeSeconds);
        public float TailDuration => Mathf.Max(0f, _tailDuration);
        public bool IsValid =>
            _effectPrefab != null &&
            EndTimeSeconds > StartTimeSeconds;

        public void Validate()
        {
            _startTimeSeconds = Mathf.Max(0f, _startTimeSeconds);
            _endTimeSeconds = Mathf.Max(
                _startTimeSeconds + 0.01f,
                _endTimeSeconds);
            _tailDuration = Mathf.Max(0f, _tailDuration);
        }
    }
}
