using UnityEngine;

namespace Alpha.Boss
{
    // Forward Butt·Stationary Jump·Target Leap를 각각 에셋으로 구성하는 예시다.
    [CreateAssetMenu(menuName = "Alpha/Boss/Pattern/Rush", fileName = "BossRushPattern")]
    public sealed class BossRushPatternDefinition : BossPatternDefinition
    {
        [SerializeField, Tooltip("이 패턴이 사용할 Rush 타입의 이동 설정입니다.")]
        private BossRushSettings _rush = new();
        [SerializeField, Tooltip("공격 실행 중 충돌하면 후딜로 전환할지 정합니다. 실행 로직 연결 시 사용합니다.")]
        private bool _stopOnCollision = true;

        public override EBossAttackType AttackType => EBossAttackType.Rush;
        public BossRushSettings Rush => _rush;
        public bool StopOnCollision => _stopOnCollision;

        protected override void OnValidate()
        {
            base.OnValidate();
            _rush ??= new();
            _rush.Validate();
        }
    }
}
