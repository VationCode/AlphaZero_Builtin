using System;
using Alpha.Combat;
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

        [SerializeField]
        private DamageProfile _damageProfile = new();

        [SerializeField]
        private EnemyAttackAreaSetting _meleeArea = new();

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

        [Tooltip(
            "공격 애니메이션 시작 후 Projectile을 발사할 시간 목록입니다. " +
            "비어 있으면 공격 시작 시 한 번 발사합니다.")]
        [SerializeField]
        private float[] _projectileFireTimesSeconds = Array.Empty<float>();

        [Tooltip(
            "Rush 애니메이션 시작 후 수평 이동을 시작할 점프 시점(초)입니다. " +
            "이 시점 전에는 제자리에서 준비 동작을 재생합니다.")]
        [SerializeField, Min(0f)]
        private float _rushJumpStartTimeSeconds = 0.53f;

        [Tooltip(
            "Rush 애니메이션 시작 후 목표 위치에 도착할 착지 시점(초)입니다. " +
            "실제 애니메이션 길이보다 길면 애니메이션 종료 시점으로 제한됩니다.")]
        [SerializeField, Min(0.01f)]
        private float _rushLandingTimeSeconds = 2.1f;

        [Tooltip(
            "Rush 시작부터 착지 시점까지의 진행률을 실제 이동 진행률로 변환합니다. " +
            "X는 착지 진행률, Y는 이동 진행률입니다.")]
        [SerializeField]
        private AnimationCurve _rushMovementCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [FormerlySerializedAs("_rushDistance")]
        [Tooltip(
            "타겟이 없는 Editor Gizmo에서 Rush 경로를 표시할 거리입니다. " +
            "실제 이동 목적지는 공격 시작 시점의 타겟 위치입니다.")]
        [SerializeField, Min(0.01f)]
        private float _rushPreviewDistance = 5f;

        [SerializeField]
        private EnemyAttackAreaSetting _rushArea = new();

        [Tooltip("보스 중심에서 즉시 판정할 Area 공격 범위입니다.")]
        [SerializeField]
        private EnemyAttackAreaSetting _areaAttackArea = new();

        [Tooltip("전장 규모로 판정할 Arena 공격 범위입니다.")]
        [SerializeField]
        private EnemyAttackAreaSetting _arenaAttackArea = new();

        public string PatternName => _patternName;
        public EEnemyAttackType AttackType => _attackType;
        public float LegacyMinimumDistance =>
            _legacyMinimumDistance;
        public float LegacySelectionWeight =>
            _legacySelectionWeight;
        public int AnimationIndex => _animationIndex;
        public DamageProfile DamageProfile => _damageProfile;
        public EnemyAttackAreaSetting MeleeArea => _meleeArea;
        public EEnemyRangeDirectionType RangeDirectionType =>
            _rangeDirectionType;
        public Transform ProjectileSpawnPoint => _projectileSpawnPoint;
        public int ProjectileSpawnPointSlotCount =>
            1 + (_additionalProjectileSpawnPoints?.Length ?? 0);
        public float ProjectileMaximumDistance =>
            _projectileMaximumDistance;
        public ProjectileEntity ProjectilePrefab => _projectilePrefab;
        public int ProjectileFireTimeCount =>
            _projectileFireTimesSeconds?.Length ?? 0;
        public float RushJumpStartTimeSeconds =>
            Mathf.Max(0f, _rushJumpStartTimeSeconds);
        public float RushLandingTimeSeconds =>
            _rushLandingTimeSeconds > 0f
                ? _rushLandingTimeSeconds
                : 2.1f;
        public float RushPreviewDistance => _rushPreviewDistance;
        public EnemyAttackAreaSetting RushArea => _rushArea;
        public EnemyAttackAreaSetting AreaAttackArea =>
            _areaAttackArea;
        public EnemyAttackAreaSetting ArenaAttackArea =>
            _arenaAttackArea;

        public float GetProjectileFireTime(int p_index)
        {
            return p_index >= 0 && p_index < ProjectileFireTimeCount
                ? Mathf.Max(0f, _projectileFireTimesSeconds[p_index])
                : 0f;
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

        // Rush 애니메이션 진행률에 대응하는 이동 진행률을 반환한다.
        public float EvaluateRushMovement(float p_normalizedTime)
        {
            float normalizedTime = Mathf.Clamp01(p_normalizedTime);

            if (normalizedTime <= 0f)
                return 0f;

            if (normalizedTime >= 1f)
                return 1f;

            if (_rushMovementCurve == null ||
                _rushMovementCurve.length == 0)
            {
                return Mathf.SmoothStep(0f, 1f, normalizedTime);
            }

            return Mathf.Clamp01(
                _rushMovementCurve.Evaluate(normalizedTime));
        }

        public bool IsExecutable =>
            _damageProfile != null &&
            _damageProfile.IsValid &&
            (_attackType switch
            {
                EEnemyAttackType.Melee =>
                    _meleeArea?.IsExecutable == true,

                EEnemyAttackType.Range =>
                    _projectileMaximumDistance > 0f &&
                    _projectilePrefab != null &&
                    _projectilePrefab.IsConfigurationValid,

                EEnemyAttackType.Rush =>
                    _rushArea?.IsExecutable == true,

                EEnemyAttackType.Area =>
                    _areaAttackArea?.IsExecutable == true,

                EEnemyAttackType.Arena =>
                    _arenaAttackArea?.IsExecutable == true,

                _ => false
            });

        // 중첩 직렬화 데이터는 소유 MonoBehaviour의 OnValidate에서 보정한다.
        public void Validate()
        {
            _patternName ??= string.Empty;
            _animationIndex = Mathf.Max(-1, _animationIndex);
            _additionalProjectileSpawnPoints ??=
                Array.Empty<Transform>();
            _projectileFireTimesSeconds ??= Array.Empty<float>();

            for (int index = 0;
                 index < _projectileFireTimesSeconds.Length;
                 index++)
            {
                _projectileFireTimesSeconds[index] = Mathf.Max(
                    0f,
                    _projectileFireTimesSeconds[index]);
            }

            _damageProfile ??= new DamageProfile();
            _meleeArea ??= new EnemyAttackAreaSetting();
            _rushArea ??= new EnemyAttackAreaSetting();
            _areaAttackArea ??= new EnemyAttackAreaSetting();
            _arenaAttackArea ??= new EnemyAttackAreaSetting();

            _damageProfile.Validate();
            _meleeArea.Validate();
            _rushArea.Validate();
            _areaAttackArea.Validate();
            _arenaAttackArea.Validate();

            _projectileMaximumDistance = Mathf.Max(
                0.01f,
                _projectileMaximumDistance);

            _rushMovementCurve ??=
                AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            _rushJumpStartTimeSeconds = Mathf.Max(
                0f,
                _rushJumpStartTimeSeconds);

            if (_rushLandingTimeSeconds <= 0f)
                _rushLandingTimeSeconds = 2.1f;

            _rushLandingTimeSeconds = Mathf.Max(
                _rushJumpStartTimeSeconds + 0.01f,
                _rushLandingTimeSeconds);

            _rushPreviewDistance = Mathf.Max(
                0.01f,
                _rushPreviewDistance);
        }

    }
}
