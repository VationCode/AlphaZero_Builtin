using System;
using Alpha.Combat;
using Alpha.Detection;
using UnityEngine;
using UnityEngine.Serialization;
using ProjectileEntity = Alpha.Projectile.Projectile;

namespace Alpha.Enemy
{
    // Range Projectile이 사용할 월드 발사 방향의 계산 기준이다.
    public enum EEnemyRangeDirectionType
    {
        Target = 0,
        FirePositionForward = 1
    }

    // 하나의 공격 패턴이 애니메이션, 피해와 타입별 실행 설정을 보관한다.
    [Serializable]
    public sealed class EnemyAttackPatternSetting
    {
        [SerializeField]
        private string _patternName = "Attack";

        [SerializeField]
        private EEnemyAttackType _attackType;

        // 기존 패턴 배열을 거리 규칙으로 이전하기 위한 직렬화 값이다.
        [FormerlySerializedAs("_minimumDistance")]
        [SerializeField, HideInInspector]
        private float _legacyMinimumDistance;

        [FormerlySerializedAs("_selectionWeight")]
        [SerializeField, HideInInspector]
        private float _legacySelectionWeight = 1f;

        [Tooltip(
            "EnemyAnimationView의 공격 애니메이션 설정과 연결할 인덱스입니다. " +
            "-1이면 해당 공격 타입의 기본 애니메이션을 사용합니다.")]
        [SerializeField, Min(-1)]
        private int _animationIndex = -1;

        [Tooltip(
            "공격 애니메이션 진행 중 발사체 또는 공격 Collider를 실행할 복수 타이밍입니다.")]
        [SerializeField]
        private EnemyAttackTimingSetting[] _attackTimings =
            Array.Empty<EnemyAttackTimingSetting>();

        [SerializeField]
        private DamageProfile _damageProfile = new();

        [SerializeField]
        private DetectionAreaSettings _meleeArea = new();

        [Tooltip(
            "Target은 각 FirePos에서 현재 타겟을 조준하고, " +
            "Fire Position Forward는 각 FirePos의 로컬 +Z 방향으로 발사합니다.")]
        [SerializeField]
        private EEnemyRangeDirectionType _rangeDirectionType =
            EEnemyRangeDirectionType.Target;

        [Tooltip("기존 단일 발사 위치이자 첫 번째 FirePos입니다.")]
        [SerializeField]
        private Transform _projectileSpawnPoint;

        [Tooltip(
            "기본 FirePos와 같은 공격 타이밍에 함께 발사할 추가 FirePos입니다.")]
        [SerializeField]
        private Transform[] _additionalProjectileSpawnPoints =
            Array.Empty<Transform>();

        [Tooltip("Range Projectile이 발사점에서 이동할 수 있는 최대 거리입니다.")]
        [FormerlySerializedAs("_maximumDistance")]
        [SerializeField, Min(0.01f)]
        private float _projectileMaximumDistance = 20f;

        [Tooltip("속도, 중력, 충돌 Layer와 피해 Collider를 직접 가진 Projectile Prefab입니다.")]
        [SerializeField]
        private ProjectileEntity _projectilePrefab;

        [SerializeField, Min(0.01f)]
        private float _rushSpeed = 8f;

        [SerializeField, Min(0.01f)]
        private float _rushDistance = 5f;

        [SerializeField]
        private DetectionAreaSettings _rushArea = new();

        [Tooltip("보스 중심에서 즉시 판정할 Area 공격 범위입니다.")]
        [SerializeField]
        private DetectionAreaSettings _areaAttackArea = new();

        [Tooltip("전장 규모로 판정할 Arena 공격 범위입니다.")]
        [SerializeField]
        private DetectionAreaSettings _arenaAttackArea = new();

        public string PatternName => _patternName;
        public EEnemyAttackType AttackType => _attackType;
        public float LegacyMinimumDistance =>
            _legacyMinimumDistance;
        public float LegacySelectionWeight =>
            _legacySelectionWeight;
        public int AnimationIndex => _animationIndex;
        public int AttackTimingCount => _attackTimings?.Length ?? 0;
        public DamageProfile DamageProfile => _damageProfile;
        public DetectionAreaSettings MeleeArea => _meleeArea;
        public EEnemyRangeDirectionType RangeDirectionType =>
            _rangeDirectionType;
        public Transform ProjectileSpawnPoint => _projectileSpawnPoint;
        public int ProjectileSpawnPointSlotCount =>
            1 + (_additionalProjectileSpawnPoints?.Length ?? 0);
        public float ProjectileMaximumDistance =>
            _projectileMaximumDistance;
        public ProjectileEntity ProjectilePrefab => _projectilePrefab;
        public float RushSpeed => _rushSpeed;
        public float RushDistance => _rushDistance;
        public DetectionAreaSettings RushArea => _rushArea;
        public DetectionAreaSettings AreaAttackArea =>
            _areaAttackArea;
        public DetectionAreaSettings ArenaAttackArea =>
            _arenaAttackArea;

        public EnemyAttackTimingSetting GetAttackTiming(int p_index)
        {
            return p_index >= 0 && p_index < AttackTimingCount
                ? _attackTimings[p_index]
                : null;
        }

        // 첫 슬롯은 기존 SpawnPoint이며 이후 슬롯은 추가 FirePos 배열을 반환한다.
        public Transform GetProjectileSpawnPoint(int p_index)
        {
            if (p_index == 0)
                return _projectileSpawnPoint;

            int additionalIndex = p_index - 1;

            return additionalIndex >= 0 &&
                   additionalIndex <
                       (_additionalProjectileSpawnPoints?.Length ?? 0)
                ? _additionalProjectileSpawnPoints[additionalIndex]
                : null;
        }

        public bool HasExecutableTiming(
            EEnemyAttackTimingType p_eventType)
        {
            for (int index = 0; index < AttackTimingCount; index++)
            {
                EnemyAttackTimingSetting timing =
                    _attackTimings[index];

                if (timing != null &&
                    timing.EventType == p_eventType &&
                    timing.IsExecutable(_attackType))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsExecutable =>
            _damageProfile != null &&
            _damageProfile.IsValid &&
            (_attackType switch
            {
                EEnemyAttackType.Melee =>
                    HasExecutableTiming(
                        EEnemyAttackTimingType.Collider) ||
                    (_meleeArea != null && _meleeArea.IsValid),

                EEnemyAttackType.Range =>
                    _projectileMaximumDistance > 0f &&
                    _projectilePrefab != null &&
                    _projectilePrefab.IsConfigurationValid,

                EEnemyAttackType.Rush =>
                    _rushSpeed > 0f &&
                    _rushDistance > 0f &&
                    (HasExecutableTiming(
                         EEnemyAttackTimingType.Collider) ||
                     (_rushArea != null && _rushArea.IsValid)),

                EEnemyAttackType.Area =>
                    HasExecutableTiming(
                        EEnemyAttackTimingType.Collider) ||
                    (_areaAttackArea != null &&
                     _areaAttackArea.IsValid),

                EEnemyAttackType.Arena =>
                    HasExecutableTiming(
                        EEnemyAttackTimingType.Collider) ||
                    (_arenaAttackArea != null &&
                     _arenaAttackArea.IsValid),

                _ => false
            });

        // 중첩 직렬화 데이터는 소유 MonoBehaviour의 OnValidate에서 보정한다.
        public void Validate()
        {
            _patternName ??= string.Empty;
            _animationIndex = Mathf.Max(-1, _animationIndex);
            _attackTimings ??=
                Array.Empty<EnemyAttackTimingSetting>();
            _additionalProjectileSpawnPoints ??=
                Array.Empty<Transform>();

            for (int index = 0;
                 index < _attackTimings.Length;
                 index++)
            {
                _attackTimings[index] ??=
                    new EnemyAttackTimingSetting();
                _attackTimings[index].Validate();
            }

            _damageProfile ??= new DamageProfile();
            _meleeArea ??= new DetectionAreaSettings();
            _rushArea ??= new DetectionAreaSettings();
            _areaAttackArea ??= new DetectionAreaSettings();
            _arenaAttackArea ??= new DetectionAreaSettings();

            _damageProfile.Validate();
            _meleeArea.Validate();
            _rushArea.Validate();
            _areaAttackArea.Validate();
            _arenaAttackArea.Validate();

            _projectileMaximumDistance = Mathf.Max(
                0.01f,
                _projectileMaximumDistance);

            _rushSpeed = Mathf.Max(0.01f, _rushSpeed);
            _rushDistance = Mathf.Max(0.01f, _rushDistance);
        }

    }
}
