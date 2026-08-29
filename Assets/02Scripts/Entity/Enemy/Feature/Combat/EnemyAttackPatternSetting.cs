using System;
using Alpha.Combat;
using Alpha.Detection;
using Alpha.Projectile;
using UnityEngine;

namespace Alpha.Enemy
{
    // 하나의 공격 패턴이 선택 조건과 타입별 실행 설정을 함께 보관한다.
    [Serializable]
    public sealed class EnemyAttackPatternSetting
    {
        [SerializeField]
        private string _patternName = "Attack";

        [SerializeField]
        private EEnemyAttackType _attackType;

        [SerializeField, Min(0f)]
        private float _minimumDistance;

        [SerializeField, Min(0.01f)]
        private float _maximumDistance = 2f;

        [SerializeField, Min(0f)]
        private float _cooldown = 1f;

        // 사용 가능한 패턴들의 Weight 합계를 기준으로 선택 비율을 결정한다.
        // 예: 두 패턴의 Weight가 1, 3이면 각각 25%, 75% 확률로 선택된다.
        [Tooltip(
            "거리와 쿨타임 조건을 만족한 패턴 사이의 선택 비율입니다. " +
            "예: Weight 1과 3은 각각 25%, 75% 확률로 선택됩니다.")]
        [SerializeField, Min(0.01f)]
        private float _selectionWeight = 1f;

        [Tooltip(
            "EnemyAnimationView의 공격 애니메이션 설정과 연결할 인덱스입니다. " +
            "-1이면 해당 공격 타입의 기본 애니메이션을 사용합니다.")]
        [SerializeField, Min(-1)]
        private int _animationIndex = -1;

        // 공격 선택과 Animation 시작 후 실제 피해·발사·돌진이 실행되기까지의 선딜레이다.
        [Tooltip(
            "공격 시작 후 실제 Melee 판정, Projectile 발사 또는 Rush가 " +
            "실행되기까지의 선딜레이(초)입니다.")]
        [SerializeField, Min(0f)]
        private float _windupDuration = 0.2f;

        // 실제 공격이 끝난 뒤 현재 공격을 종료하고 다음 행동을 허용하기까지의 후딜레이다.
        // Recovery 중에는 다른 공격 패턴도 시작할 수 없다.
        [Tooltip(
            "실제 공격 실행 후 현재 공격이 종료되기까지의 후딜레이(초)입니다. " +
            "이 시간 동안에는 다른 공격 패턴도 시작할 수 없습니다.")]
        [SerializeField, Min(0f)]
        private float _attackDelay = 0.5f;

        [SerializeField]
        private DamageProfile _damageProfile = new();

        [SerializeField]
        private DetectionAreaSettings _meleeArea = new();

        [SerializeField]
        private Transform _projectileSpawnPoint;

        [SerializeField]
        private ProjectileLaunchSettings _projectileLaunchSettings = new(
            null,
            20f,
            (LayerMask)65);

        [SerializeField, Min(0.01f)]
        private float _rushSpeed = 8f;

        [SerializeField, Min(0.01f)]
        private float _rushDistance = 5f;

        [SerializeField, Min(0.01f)]
        private float _rushDuration = 0.5f;

        [SerializeField]
        private DetectionAreaSettings _rushArea = new();

        public string PatternName => _patternName;
        public EEnemyAttackType AttackType => _attackType;
        public float MinimumDistance => _minimumDistance;
        public float MaximumDistance =>
            _attackType == EEnemyAttackType.Melee
                ? _meleeArea?.MaximumHorizontalReach ?? 0f
                : _maximumDistance;
        public float Cooldown => _cooldown;
        public float SelectionWeight => _selectionWeight;
        public int AnimationIndex => _animationIndex;
        public float WindupDuration => _windupDuration;
        public float RecoveryDuration => _attackDelay;
        public DamageProfile DamageProfile => _damageProfile;
        public DetectionAreaSettings MeleeArea => _meleeArea;
        public Transform ProjectileSpawnPoint => _projectileSpawnPoint;
        public ProjectileLaunchSettings ProjectileLaunchSettings =>
            _projectileLaunchSettings;
        public float RushSpeed => _rushSpeed;
        public float RushDistance => _rushDistance;
        public float RushDuration => _rushDuration;
        public DetectionAreaSettings RushArea => _rushArea;

        public bool IsExecutable =>
            _damageProfile != null &&
            _damageProfile.IsValid &&
            MaximumDistance >= _minimumDistance &&
            (_attackType switch
            {
                EEnemyAttackType.Melee =>
                    _meleeArea != null && _meleeArea.IsValid,

                EEnemyAttackType.Range =>
                    _projectileLaunchSettings.IsValid &&
                    _projectileLaunchSettings.Prefab
                        .IsConfigurationValid,

                EEnemyAttackType.Rush =>
                    _rushArea != null &&
                    _rushArea.IsValid &&
                    _rushSpeed > 0f &&
                    _rushDistance > 0f &&
                    _rushDuration > 0f,

                _ => false
            });

        public bool IsWithinDistance(float p_distance)
        {
            return p_distance >= _minimumDistance &&
                   p_distance <= MaximumDistance;
        }

        // 중첩 직렬화 데이터는 소유 MonoBehaviour의 OnValidate에서 보정한다.
        public void Validate()
        {
            _patternName ??= string.Empty;
            _minimumDistance = Mathf.Max(0f, _minimumDistance);
            _maximumDistance = Mathf.Max(
                Mathf.Max(0.01f, _minimumDistance),
                _maximumDistance);
            _cooldown = Mathf.Max(0f, _cooldown);
            _selectionWeight = Mathf.Max(0.01f, _selectionWeight);
            _animationIndex = Mathf.Max(-1, _animationIndex);
            _windupDuration = Mathf.Max(0f, _windupDuration);
            _attackDelay = Mathf.Max(0f, _attackDelay);

            _damageProfile ??= new DamageProfile();
            _meleeArea ??= new DetectionAreaSettings();
            _rushArea ??= new DetectionAreaSettings();

            _damageProfile.Validate();
            _meleeArea.Validate();
            _rushArea.Validate();

            // Melee는 실제 판정 영역을 최대 공격 거리의 단일 기준으로 사용한다.
            if (_attackType == EEnemyAttackType.Melee)
            {
                _maximumDistance =
                    _meleeArea.MaximumHorizontalReach;
            }

            _projectileLaunchSettings.Validate();

            _rushSpeed = Mathf.Max(0.01f, _rushSpeed);
            _rushDistance = Mathf.Max(0.01f, _rushDistance);
            _rushDuration = Mathf.Max(0.01f, _rushDuration);
        }

    }
}
