using System;
using Alpha.Combat;
using UnityEngine;

namespace Alpha.Boss
{
    // 모든 공격 설정의 공통 부모다. 실행과 선택 이력은 Module·Flow·Context가 소유한다.
    [Serializable]
    public abstract class BossAttackSettings
    {
        [SerializeField, HideInInspector] private string _id = Guid.NewGuid().ToString("N");
        [SerializeField] private string _patternName = "Attack";
        [SerializeField] private string _animationKey = string.Empty;
        [SerializeField] private BossPatternSelectionSettings _selection = new();
        [SerializeField] private DamageProfile _damageProfile = new();

        public string Id => _id;
        public string PatternName => _patternName;
        public string AnimationKey => _animationKey;
        public BossPatternSelectionSettings Selection => _selection;
        public DamageProfile DamageProfile => _damageProfile;
        public abstract EBossAttackType AttackType { get; }

        public virtual void Validate()
        {
            if (string.IsNullOrWhiteSpace(_id))
                RegenerateId();
            _patternName = _patternName?.Trim() ?? string.Empty;
            _animationKey = _animationKey?.Trim() ?? string.Empty;
            _selection ??= new BossPatternSelectionSettings();
            _selection.Validate();
            _damageProfile ??= new DamageProfile();
            _damageProfile.Validate();
        }

        internal void RegenerateId() => _id = Guid.NewGuid().ToString("N");

        // 종류를 변경해도 패턴 ID와 공통 설정은 유지한다.
        public void CopyCommonFrom(BossAttackSettings p_source)
        {
            if (p_source != null)
                RestoreCommon(p_source.Id, p_source.PatternName, p_source.AnimationKey,
                    p_source.Selection, p_source.DamageProfile);
        }

        internal void RestoreCommon(string p_id, string p_name, string p_animation,
            BossPatternSelectionSettings p_selection, DamageProfile p_damage)
        {
            _id = p_id;
            _patternName = p_name;
            _animationKey = p_animation;
            _selection = p_selection ?? new BossPatternSelectionSettings();
            _damageProfile = p_damage ?? new DamageProfile();
        }

        public static BossAttackSettings Create(EBossAttackType p_type) => p_type switch
        {
            EBossAttackType.Range => new BossRangeAttackSettings(),
            EBossAttackType.Area => new BossAoEAttackSettings(),
            EBossAttackType.Arena => new BossAoEAttackSettings(true),
            EBossAttackType.Rush => new BossMeleeAttackSettings(true),
            _ => new BossMeleeAttackSettings()
        };

        // 같은 계열의 종류 전환은 세부 설정도 유지한다. 객체 교체는 Inspector의 Undo 대상이다.
        public static BossAttackSettings ChangeType(BossAttackSettings p_source, EBossAttackType p_type)
        {
            if (p_source != null && p_source.AttackType == p_type)
                return p_source;
            BossAttackSettings result = Create(p_type);
            result.CopyCommonFrom(p_source);
            if (result is BossMeleeAttackSettings melee && p_source is BossMeleeAttackSettings oldMelee)
                melee.CopyDetailsFrom(oldMelee);
            if (result is BossAoEAttackSettings aoe && p_source is BossAoEAttackSettings oldAoE)
                aoe.Restore(oldAoE.AreaAttack, oldAoE.ArenaAttack);
            return result;
        }
    }
}
