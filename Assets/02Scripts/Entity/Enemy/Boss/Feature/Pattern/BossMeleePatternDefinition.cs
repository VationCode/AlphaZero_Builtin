using UnityEngine;

namespace Alpha.Boss
{
    [CreateAssetMenu(menuName = "Alpha/Boss/Pattern/Melee", fileName = "BossMeleePattern")]
    public sealed class BossMeleePatternDefinition : BossPatternDefinition
    {
        [SerializeField, Tooltip("이 패턴이 사용할 Melee 타입의 공격 설정입니다. 실제 실행은 Module 연결 후 동작합니다.")]
        private BossMeleeSettings _attack = new();

        public override EBossAttackType AttackType => EBossAttackType.Melee;
        public BossMeleeSettings Attack => _attack;

        protected override void OnValidate()
        {
            base.OnValidate();
            _attack ??= new();
            _attack.Validate();
        }
    }
}

