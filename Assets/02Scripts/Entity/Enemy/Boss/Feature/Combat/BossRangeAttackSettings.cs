using System;
using UnityEngine;
using ProjectileEntity = Alpha.Projectile.Projectile;
using GroundWaveEntity = Alpha.GroundWave.GroundWave;

namespace Alpha.Boss
{
    public enum EBossRangeAttackMode
    {
        Projectile = 0,
        GroundWave = 1
    }

    // 각 발사점에서 공격의 진행 방향을 계산할 기준이다.
    public enum EBossRangeDirectionType
    {
        Target = 0,
        FirePositionForward = 1
    }

    // 보스 원거리 패턴의 공통 발사 설정과 시간별 발사 그룹을 보관한다.
    [Serializable]
    public sealed class BossRangeAttackSettings : BossAttackSettings
    {
        private const float MinimumDistance = 0.01f;

        [SerializeField]
        private EBossRangeAttackMode _attackMode;

        [SerializeField]
        [Tooltip("진행 속도, 폭발 간격과 피해 범위는 GroundWave Prefab에서 설정합니다.")]
        private GroundWaveEntity _groundWavePrefab;

        [SerializeField]
        [Tooltip("속도, 중력, 충돌과 명중 처리는 Projectile Prefab이 소유합니다.")]
        private ProjectileEntity _projectilePrefab;

        [SerializeField]
        [Tooltip("Target은 그룹 발사 순간에 확정한 타겟 위치, FirePositionForward는 각 발사점의 +Z 방향을 사용합니다.")]
        private EBossRangeDirectionType _directionType = EBossRangeDirectionType.Target;

        [SerializeField, Min(MinimumDistance)]
        [Tooltip("각 투사체가 자신의 발사 위치를 기준으로 이동할 수 있는 최대 거리입니다.")]
        private float _maximumDistance = 20f;

        [SerializeField]
        [Tooltip("각 그룹은 지정 시간에 한 번 발사합니다. 두 그룹으로 나누려면 항목 두 개에 시간과 발사점을 각각 등록합니다.")]
        private BossRangeFireGroup[] _fireGroups = Array.Empty<BossRangeFireGroup>();

        public override EBossAttackType AttackType => EBossAttackType.Range;
        public EBossRangeAttackMode AttackMode => _attackMode;
        public GroundWaveEntity GroundWavePrefab => _groundWavePrefab;
        public ProjectileEntity ProjectilePrefab => _projectilePrefab;
        public EBossRangeDirectionType DirectionType => _directionType;
        public float MaximumDistance => _maximumDistance;
        public int FireGroupCount => _fireGroups?.Length ?? 0;

        public BossRangeFireGroup GetFireGroup(int p_index)
        {
            return p_index >= 0 && p_index < FireGroupCount ? _fireGroups[p_index] : null;
        }

        public override void Validate()
        {
            base.Validate();
            if (!Enum.IsDefined(typeof(EBossRangeAttackMode), _attackMode))
                _attackMode = EBossRangeAttackMode.Projectile;
            if (!Enum.IsDefined(typeof(EBossRangeDirectionType), _directionType))
                _directionType = EBossRangeDirectionType.Target;

            _maximumDistance = Mathf.Max(MinimumDistance, _maximumDistance);

            _fireGroups ??= Array.Empty<BossRangeFireGroup>();
            for (int index = 0; index < _fireGroups.Length; index++)
            {
                _fireGroups[index] ??= new BossRangeFireGroup();
                _fireGroups[index].Validate();
            }
        }
    }
}
