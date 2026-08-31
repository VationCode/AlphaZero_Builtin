using System;
using UnityEngine;

namespace Alpha.Combat
{
    // 공격자가 전달하는 피격 종류이며 피격자는 해당 반응의 허용 여부만 결정한다.
    public enum EHitType
    {
        None = 0,
        Light = 1,
        Heavy = 2,
        Knockdown = 3,
        Launch = 4
    }

    // 공용 판정이 반환하고 Entity별 ActionFlow가 실행할 반응 단계다.
    public enum EHitReaction
    {
        None,
        Light,
        Heavy,
        Knockdown,
        Launch
    }

    // Hit Type 하나에 대응하는 넉백 실행 수치를 보관한다.
    [Serializable]
    public sealed class HitTypeKnockbackSettings
    {
        private const float MaximumDistance = 10f;
        private const float MaximumDuration = 2f;

        [Tooltip("이 공격이 피격자를 밀어낼 거리입니다.")]
        [SerializeField, Range(0f, MaximumDistance)]
        private float _distance = 1f;

        [Tooltip("설정한 넉백 거리까지 이동하는 시간입니다. 0이면 넉백하지 않습니다.")]
        [SerializeField, Range(0f, MaximumDuration)]
        private float _duration = 0.2f;

        public float Distance => _distance;
        public float Duration => _duration;

        public HitTypeKnockbackSettings()
        {
        }

        public HitTypeKnockbackSettings(
            float p_distance,
            float p_duration)
        {
            Set(p_distance, p_duration);
        }

        public void Set(
            float p_distance,
            float p_duration)
        {
            _distance = p_distance;
            _duration = p_duration;
            Validate();
        }

        public void Validate()
        {
            _distance = Mathf.Clamp(
                _distance,
                0f,
                MaximumDistance);
            _duration = Mathf.Clamp(
                _duration,
                0f,
                MaximumDuration);
        }
    }

    // 공격 하나가 선택한 Hit Type과 타입별 넉백·공통 회복 수치를 보관한다.
    [Serializable]
    public sealed class AttackImpactSettings : ISerializationCallbackReceiver
    {
        private const int CurrentKnockbackSettingsVersion = 1;

        [Tooltip("공격자가 전달할 피격 종류입니다.")]
        [SerializeField]
        private EHitType _hitType = EHitType.Light;

        [SerializeField]
        private HitTypeKnockbackSettings _lightKnockback = new();

        [SerializeField]
        private HitTypeKnockbackSettings _heavyKnockback = new();

        [SerializeField]
        private HitTypeKnockbackSettings _knockdownKnockback = new();

        [SerializeField]
        private HitTypeKnockbackSettings _launchKnockback = new();

        // 기존 Scene과 Prefab의 단일 넉백 값을 선택된 Hit Type으로 이전하기 위해 유지한다.
        [SerializeField, HideInInspector]
        private float _knockbackDistance = 1f;

        [SerializeField, HideInInspector]
        private float _knockbackDuration = 0.2f;

        [SerializeField, HideInInspector]
        private int _knockbackSettingsVersion;

        [Tooltip("이 공격으로 발생한 피격 반응이 유지되는 시간입니다.")]
        [SerializeField, Min(0f)]
        private float _recoveryDuration = 0.25f;

        public EHitType HitType => _hitType;
        public float KnockbackDistance =>
            GetSelectedKnockbackSettings()?.Distance ?? 0f;
        public float KnockbackDuration =>
            GetSelectedKnockbackSettings()?.Duration ?? 0f;
        public float RecoveryDuration => _recoveryDuration;

        public AttackImpactSettings()
        {
        }

        public AttackImpactSettings(
            EHitType p_hitType,
            float p_knockbackDistance,
            float p_knockbackDuration,
            float p_recoveryDuration)
        {
            _hitType = p_hitType;
            _knockbackDistance = p_knockbackDistance;
            _knockbackDuration = p_knockbackDuration;
            _recoveryDuration = p_recoveryDuration;
            EnsureKnockbackSettings();
            Validate();
        }

        // 새 객체와 기존 직렬화 데이터가 같은 타입별 구조로 저장되게 보장한다.
        public void OnBeforeSerialize()
        {
            EnsureKnockbackSettings();
        }

        public void OnAfterDeserialize()
        {
            EnsureKnockbackSettings();
        }

        public AttackImpactInfo CreateInfo()
        {
            HitTypeKnockbackSettings knockbackSettings =
                GetSelectedKnockbackSettings();

            return new AttackImpactInfo(
                _hitType,
                knockbackSettings?.Distance ?? 0f,
                knockbackSettings?.Duration ?? 0f,
                _recoveryDuration);
        }

        public void Validate()
        {
            if (!Enum.IsDefined(typeof(EHitType), _hitType))
                _hitType = EHitType.None;

            EnsureKnockbackSettings();
            _lightKnockback.Validate();
            _heavyKnockback.Validate();
            _knockdownKnockback.Validate();
            _launchKnockback.Validate();
            _recoveryDuration = Mathf.Max(0f, _recoveryDuration);
        }

        private HitTypeKnockbackSettings GetSelectedKnockbackSettings()
        {
            EnsureKnockbackSettings();
            return ResolveKnockbackSettings(_hitType);
        }

        private HitTypeKnockbackSettings ResolveKnockbackSettings(
            EHitType p_hitType)
        {
            return p_hitType switch
            {
                EHitType.Light => _lightKnockback,
                EHitType.Heavy => _heavyKnockback,
                EHitType.Knockdown => _knockdownKnockback,
                EHitType.Launch => _launchKnockback,
                _ => null
            };
        }

        private void EnsureKnockbackSettings()
        {
            _lightKnockback ??= new HitTypeKnockbackSettings();
            _heavyKnockback ??= new HitTypeKnockbackSettings();
            _knockdownKnockback ??= new HitTypeKnockbackSettings();
            _launchKnockback ??= new HitTypeKnockbackSettings();

            if (_knockbackSettingsVersion >=
                CurrentKnockbackSettingsVersion)
            {
                return;
            }

            // 기존 단일 값은 당시 선택되어 있던 Hit Type의 첫 프로필로 이전한다.
            ResolveKnockbackSettings(_hitType)?.Set(
                _knockbackDistance,
                _knockbackDuration);

            _knockbackSettingsVersion =
                CurrentKnockbackSettingsVersion;
        }
    }

    // DamageInfo가 공격자로부터 피격자에게 전달할 불변 충격 정보다.
    public readonly struct AttackImpactInfo
    {
        public EHitType HitType { get; }
        public float KnockbackDistance { get; }
        public float KnockbackDuration { get; }
        public float RecoveryDuration { get; }
        public bool HasImpact => HitType != EHitType.None;

        public AttackImpactInfo(
            EHitType p_hitType,
            float p_knockbackDistance,
            float p_knockbackDuration,
            float p_recoveryDuration)
        {
            HitType = p_hitType;
            KnockbackDistance = Mathf.Max(0f, p_knockbackDistance);
            KnockbackDuration = Mathf.Max(0f, p_knockbackDuration);
            RecoveryDuration = Mathf.Max(0f, p_recoveryDuration);
        }
    }

    // 피격자는 각 Hit Type의 반응을 실행할 수 있는지만 결정한다.
    [Serializable]
    public sealed class HitTypeResponseSettings
    {
        [Header("Hit Type Response")]
        [SerializeField]
        private bool _respondToLight = true;

        [SerializeField]
        private bool _respondToHeavy = true;

        [SerializeField]
        private bool _respondToKnockdown = true;

        [SerializeField]
        private bool _respondToLaunch = true;

        public bool CanRespond(EHitType p_hitType)
        {
            return p_hitType switch
            {
                EHitType.Light => _respondToLight,
                EHitType.Heavy => _respondToHeavy,
                EHitType.Knockdown => _respondToKnockdown,
                EHitType.Launch => _respondToLaunch,
                _ => false
            };
        }
    }

    // 피격 반응이 끝난 뒤 동일 Hit Type이 행동을 다시 중단할 수 없는 시간을 보관한다.
    [Serializable]
    public sealed class HitReactionImmunitySettings
    {
        [Tooltip("Light 피격 반응이 끝난 뒤 동일 타입 반응을 무시할 시간입니다.")]
        [SerializeField, Min(0f)]
        private float _lightDuration = 0.35f;

        [Tooltip("Heavy 피격 반응이 끝난 뒤 동일 타입 반응을 무시할 시간입니다.")]
        [SerializeField, Min(0f)]
        private float _heavyDuration = 0.6f;

        [Tooltip("Knockdown 피격 반응이 끝난 뒤 동일 타입 반응을 무시할 시간입니다.")]
        [SerializeField, Min(0f)]
        private float _knockdownDuration = 1.5f;

        [Tooltip("Launch 피격 반응이 끝난 뒤 동일 타입 반응을 무시할 시간입니다.")]
        [SerializeField, Min(0f)]
        private float _launchDuration = 1.5f;

        public float GetDuration(EHitReaction p_reaction)
        {
            return p_reaction switch
            {
                EHitReaction.Light => _lightDuration,
                EHitReaction.Heavy => _heavyDuration,
                EHitReaction.Knockdown => _knockdownDuration,
                EHitReaction.Launch => _launchDuration,
                _ => 0f
            };
        }

        public void Validate()
        {
            _lightDuration = Mathf.Max(0f, _lightDuration);
            _heavyDuration = Mathf.Max(0f, _heavyDuration);
            _knockdownDuration = Mathf.Max(0f, _knockdownDuration);
            _launchDuration = Mathf.Max(0f, _launchDuration);
        }
    }

    // 공용 판정 결과는 피격자가 실행할 반응과 공격자가 전달한 수치를 보관한다.
    public readonly struct ImpactReactionResult
    {
        public EHitReaction Reaction { get; }
        public float RecoveryDuration { get; }
        public float KnockbackDistance { get; }
        public float KnockbackDuration { get; }
        public int Priority => (int)Reaction;
        public bool HasReaction => Reaction != EHitReaction.None;
        public bool HasKnockback =>
            KnockbackDistance > 0f &&
            KnockbackDuration > 0f;

        public ImpactReactionResult(
            EHitReaction p_reaction,
            float p_recoveryDuration,
            float p_knockbackDistance,
            float p_knockbackDuration)
        {
            Reaction = p_reaction;
            RecoveryDuration = Mathf.Max(0f, p_recoveryDuration);
            KnockbackDistance = Mathf.Max(0f, p_knockbackDistance);
            KnockbackDuration = Mathf.Max(0f, p_knockbackDuration);
        }
    }
}
