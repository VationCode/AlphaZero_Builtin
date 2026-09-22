using UnityEngine;

namespace Alpha.Boss
{
    // 공격 타입은 실행 방식이다. 근거리·중거리·원거리 분류로 사용하지 않는다.
    public enum EBossAttackType { Melee, Rush, Range, GlobalAoE }

    // 개별 패턴 에셋의 공통 틀이다. 타입별 설정은 구체 Definition이 소유한다.
    public abstract class BossPatternDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("ID·사용 거리·쿨다운·선택 가중치입니다.")]
        private BossPatternSettings _common = new();
        [SerializeField, Tooltip("Active 단계에서 재생할 공격 클립입니다. 실행 시간에 맞춰 View가 재생 속도를 조절합니다.")]
        private AnimationClip _animationClip;
        [SerializeField, Min(0f), Tooltip("공격 클립 재생 전 대기 시간(초)입니다. 공격 재생 속도에는 영향을 주지 않습니다.")]
        private float _prepareDuration = 0.5f;
        [SerializeField, Min(0f), Tooltip("공격 클립을 재생할 시간(초)입니다. 속도는 원본 클립 길이 / 실행 시간이며, 0이면 재생을 건너뜁니다.")]
        private float _activeDuration = 1f;
        [SerializeField, Min(0f), Tooltip("공격 클립 종료 후 대기 시간(초)입니다. 공격 재생 속도에는 영향을 주지 않습니다.")]
        private float _recoveryDuration = 0.5f;

        public abstract EBossAttackType AttackType { get; }
        public BossPatternSettings Common => _common;
        public AnimationClip AnimationClip => _animationClip;
        public float PrepareDuration => _prepareDuration;
        public float ActiveDuration => _activeDuration;
        public float RecoveryDuration => _recoveryDuration;

        protected virtual void OnValidate()
        {
            _common ??= new();
            _common.Validate();
            _prepareDuration = BossPatternSettings.NonNegative(_prepareDuration);
            _activeDuration = BossPatternSettings.NonNegative(_activeDuration);
            _recoveryDuration = BossPatternSettings.NonNegative(_recoveryDuration);
        }
    }
}
