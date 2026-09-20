using System;
using UnityEngine;

namespace Alpha.Boss
{
    public enum EBossRushEffectTiming { AttackStart, MovementStart, MovementEnd }

    // Prefab은 표현만 담당하며 피해량과 판정 영역은 패턴의 Damage 설정이 소유한다.
    [Serializable]
    public sealed class BossRushEffectSettings
    {
        [SerializeField] private EBossRushEffectTiming _timing = EBossRushEffectTiming.MovementEnd;
        [SerializeField] private GameObject _effectPrefab;
        [Tooltip("비어 있으면 보스 위치에서 생성합니다.")]
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private Vector3 _localOffset;
        [SerializeField] private Vector3 _localEulerAngles;
        [SerializeField] private bool _followSpawnPoint;
        [Tooltip("생성 후 이 시간 동안 유지합니다. 공격 취소·사망 시에는 즉시 정리합니다.")]
        [SerializeField, Min(0.01f)] private float _lifetime = 3f;

        public EBossRushEffectTiming Timing => _timing;
        public GameObject EffectPrefab => _effectPrefab;
        public Transform SpawnPoint => _spawnPoint;
        public Vector3 LocalOffset => _localOffset;
        public Vector3 LocalEulerAngles => _localEulerAngles;
        public bool FollowSpawnPoint => _followSpawnPoint;
        public float Lifetime => _lifetime;
        public bool IsValid => _effectPrefab != null && _lifetime > 0f && !float.IsInfinity(_lifetime);

        public void Validate()
        {
            if (!Enum.IsDefined(typeof(EBossRushEffectTiming), _timing))
                _timing = EBossRushEffectTiming.MovementEnd;
            _lifetime = float.IsNaN(_lifetime) || float.IsInfinity(_lifetime) ? 3f : Mathf.Max(0.01f, _lifetime);
        }
    }
}
