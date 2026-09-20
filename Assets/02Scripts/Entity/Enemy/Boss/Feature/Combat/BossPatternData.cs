using System;
using Alpha.Combat;
using UnityEngine;

namespace Alpha.Boss
{
    // 패턴의 참조 단위다. 실제 설정은 공통 부모를 상속한 하나의 객체가 소유한다.
    [Serializable]
    public sealed class BossPatternData
    {
        [SerializeReference] private BossAttackSettings _settings;

        // Scene 표시 상태는 공격 설정과 분리한다.
        [SerializeField] private bool _showAttackRange = true;
        [SerializeField] private bool _showMinimumDistance;
        [SerializeField] private bool _showMaximumDistance;

        public BossAttackSettings Settings => _settings ??= RestoreLegacySettings();
        public string Id => Settings.Id;
        public string PatternName => Settings.PatternName;
        public EBossAttackType AttackType => Settings.AttackType;
        public string AnimationKey => Settings.AnimationKey;
        public BossPatternSelectionSettings Selection => Settings.Selection;
        public DamageProfile DamageProfile => Settings.DamageProfile;
        public bool ShowAttackRange => _showAttackRange;
        public bool ShowMinimumDistance => _showMinimumDistance;
        public bool ShowMaximumDistance => _showMaximumDistance;

        // 기존 Module의 실행 계약을 유지한다. 다른 공격 종류의 설정은 반환하지 않는다.
        public BossDirectHitSettings Damage => (Settings as BossMeleeAttackSettings)?.DirectHit;
        public BossMovementAttackSettings MovementAttack => (Settings as BossMeleeAttackSettings)?.MovementAttack;
        public BossRushAttackSettings RushAttack => (Settings as BossMeleeAttackSettings)?.RushAttack;
        public BossRangeAttackSettings RangeAttack => Settings as BossRangeAttackSettings;
        public BossAreaAttackSettings AreaAttack => (Settings as BossAoEAttackSettings)?.AreaAttack;
        public BossArenaAttackSettings ArenaAttack => (Settings as BossAoEAttackSettings)?.ArenaAttack;

        internal void RegenerateId() => Settings.RegenerateId();
        public void Validate() => Settings.Validate();

        // Unity 배열 복제는 managed reference를 공유할 수 있다. 원본의 ID·값을 바꾸지 않도록 분리한다.
        internal void MakeSettingsIndependent()
        {
            BossAttackSettings source = Settings;
            _settings = (BossAttackSettings)JsonUtility.FromJson(JsonUtility.ToJson(source), source.GetType());
        }

        // 기존 Scene·Prefab 저장값을 읽는 호환 필드다. 새 Inspector와 실행에서는 사용하지 않는다.
        [SerializeField, HideInInspector] private string _id = Guid.NewGuid().ToString("N");
        [SerializeField, HideInInspector] private string _patternName = "Attack";
        [SerializeField, HideInInspector] private EBossAttackType _attackType;
        [SerializeField, HideInInspector] private string _animationKey = string.Empty;
        [SerializeField, HideInInspector] private BossPatternSelectionSettings _selection = new();
        [SerializeField, HideInInspector] private BossDamageSettings _damage = new();
        [SerializeField, HideInInspector] private BossMovementAttackSettings _movementAttack = new();
        [SerializeField, HideInInspector] private BossRushAttackSettings _rushAttack = new();
        [SerializeField, HideInInspector] private BossRangeAttackSettings _rangeAttack = new();
        [SerializeField, HideInInspector] private BossAreaAttackSettings _areaAttack = new();
        [SerializeField, HideInInspector] private BossArenaAttackSettings _arenaAttack = new();

        private BossAttackSettings RestoreLegacySettings()
        {
            BossAttackSettings settings;
            switch (_attackType)
            {
                case EBossAttackType.Range:
                    settings = _rangeAttack ?? new BossRangeAttackSettings();
                    break;
                case EBossAttackType.Area:
                case EBossAttackType.Arena:
                    var aoe = new BossAoEAttackSettings(_attackType == EBossAttackType.Arena);
                    aoe.Restore(_areaAttack, _arenaAttack);
                    settings = aoe;
                    break;
                default:
                    var melee = new BossMeleeAttackSettings(_attackType == EBossAttackType.Rush);
                    melee.Restore(_damage, _movementAttack, _rushAttack);
                    settings = melee;
                    break;
            }
            settings.RestoreCommon(_id, _patternName, _animationKey, _selection, _damage?.Profile);
            return settings;
        }
    }
}
