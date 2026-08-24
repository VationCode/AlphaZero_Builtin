using System;
using Alpha.Detection;
using UnityEngine;

namespace Alpha.Combat
{
    // Entity와 관계없이 근접 Skill 한 번의 시간·피해·판정 정보를 보관한다.
    [Serializable]
    public sealed class MeleeSkillAttackSettings
    {
        [Tooltip("Skill 하나가 유지되는 전체 시간(초)입니다.")]
        [SerializeField, Min(0.01f)]
        private float _duration = 0.6f;

        [Tooltip("Skill 시작 후 실제 타격 판정을 실행할 시간(초)입니다.")]
        [SerializeField, Min(0f)]
        private float _hitTime = 0.2f;

        [Tooltip("MeleeWeapon의 기본 공격력에 곱할 Skill 배율입니다.")]
        [SerializeField, Min(0f)]
        private float _damageMultiplier = 1f;

        [SerializeField]
        private AttackImpactSettings _impactSettings = new();

        [SerializeField]
        private DetectionAreaSettings _area = new();

        public float Duration => _duration;
        public float HitTime => _hitTime;
        public float DamageMultiplier => _damageMultiplier;
        public AttackImpactInfo Impact =>
            _impactSettings?.CreateInfo() ?? default;
        public DetectionAreaSettings Area => _area;

        public bool IsValid =>
            _duration > 0f &&
            _hitTime >= 0f &&
            _hitTime <= _duration &&
            _damageMultiplier > 0f &&
            _impactSettings != null &&
            _area != null &&
            _area.IsValid;

        public void Validate()
        {
            _duration = Mathf.Max(0.01f, _duration);
            _hitTime = Mathf.Clamp(_hitTime, 0f, _duration);
            _damageMultiplier = Mathf.Max(0f, _damageMultiplier);
            _impactSettings ??= new AttackImpactSettings();
            _impactSettings.Validate();
            _area ??= new DetectionAreaSettings();
            _area.Validate();
        }
    }

    // 공통 Skill 표현 데이터에 근접 공격 설정만 확장한다.
    [CreateAssetMenu(
        fileName = "MeleeSkill",
        menuName = "Alpha/Combat/Skill/Melee")]
    public sealed class MeleeSkillDefinition : CombatSkillDefinition
    {
        [Tooltip("설정된 재생 시간에 Player의 Combat Effect 아래에 생성할 Prefab입니다.")]
        [SerializeField]
        private GameObject _effectPrefab;

        [Tooltip("Skill 시작 후 Effect 재생을 요청할 시간(초)입니다.")]
        [SerializeField, Min(0f)]
        private float _effectStartTime;

        [SerializeField]
        private MeleeSkillAttackSettings _attackSettings = new();

        public GameObject EffectPrefab => _effectPrefab;
        public float EffectStartTime => _effectStartTime;
        public float EffectLifetime => Mathf.Max(
            0.01f,
            _attackSettings.Duration - _effectStartTime);
        public MeleeSkillAttackSettings AttackSettings => _attackSettings;

        public override bool IsValid =>
            base.IsValid &&
            _attackSettings != null &&
            _attackSettings.IsValid &&
            _effectStartTime >= 0f &&
            _effectStartTime <= _attackSettings.Duration;

        protected override void OnValidate()
        {
            base.OnValidate();
            _attackSettings ??= new MeleeSkillAttackSettings();
            _attackSettings.Validate();
            _effectStartTime = Mathf.Clamp(
                _effectStartTime,
                0f,
                _attackSettings.Duration);
        }
    }
}
