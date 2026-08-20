using System;
using UnityEngine;

namespace Alpha.Projectile
{
    // Player 무기와 Enemy 공격 패턴이 각각 소유하는 공용 Projectile 발사 설정이다.
    [Serializable]
    public struct ProjectileLaunchSettings
    {
        [Tooltip("발사 주체가 생성할 Projectile Prefab입니다.")]
        [SerializeField]
        private Projectile _prefab;

        [Tooltip("발사 주체가 Projectile에 부여할 초당 이동 속도입니다.")]
        [SerializeField, Min(0.01f)]
        private float _speed;

        [Tooltip("Projectile이 충돌 대상으로 검사할 Layer입니다.")]
        [SerializeField]
        private LayerMask _hitMask;

        public Projectile Prefab => _prefab;
        public float Speed => _speed;
        public LayerMask HitMask => _hitMask;

        public bool IsValid =>
            _prefab != null &&
            _speed > 0f;

        public ProjectileLaunchSettings(
            Projectile p_prefab,
            float p_speed,
            LayerMask p_hitMask)
        {
            _prefab = p_prefab;
            _speed = Mathf.Max(0.01f, p_speed);
            _hitMask = p_hitMask;
        }

        // 중첩 직렬화 값은 실제 소유자의 OnValidate에서 보정한다.
        public void Validate()
        {
            _speed = Mathf.Max(0.01f, _speed);
        }
    }
}
