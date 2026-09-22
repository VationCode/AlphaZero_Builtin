using System;

namespace Alpha.Boss
{
    // 한 보스의 실행 인스턴스다. 정적 설정은 별도로 보관하고 Enter에서 실행 상태를 초기화한다.
    public abstract class BossPattern
    {
        // 물리 이동의 미세한 잔여 거리 때문에 도착 직전 걷기가 유지되지 않도록 한다.
        private const float AttackArrivalTolerance = 0.05f;
        internal bool IsOwned { get; set; }
        internal bool CancellationRequested { get; set; }
        public BossPatternSettings Settings { get; }
        public BossPatternContext Runtime { get; } = new();
        public float MinDistance => Settings.MinDistance;
        public float MaxDistance => Settings.MaxDistance;
        public float AttackStartDistance => Settings.AttackStartDistance;

        // 거리 설정은 보스별 실행 인스턴스를 조립할 때 전달한다. 경계값을 포함하는 수평 거리다.
        protected BossPattern(float p_minDistance = 0f, float p_maxDistance = float.MaxValue)
            : this(new BossPatternSettings("Pattern", p_minDistance, p_maxDistance)) { }

        protected BossPattern(BossPatternSettings p_settings)
        {
            // 공유 에셋의 설정을 실행 중 변경하지 않도록 인스턴스별 스냅샷을 만든다.
            Settings = p_settings?.Copy() ?? throw new ArgumentNullException(nameof(p_settings));
        }

        public bool IsInRange(float p_distance) => !float.IsNaN(p_distance) && !float.IsInfinity(p_distance) &&
            p_distance >= MinDistance && p_distance <= MaxDistance;

        // 이동 정지와 공격 진입이 함께 사용하는 도착 판정이다. 선택 범위는 확장하지 않는다.
        public bool IsInAttackRange(float p_distance) => IsInRange(p_distance) &&
            p_distance <= AttackStartDistance + AttackArrivalTolerance;

        internal bool CanStart(BossContext p_context) => p_context != null &&
            Runtime.GetCooldownRemaining(p_context.ElapsedTime) <= 0d && CanExecute(p_context);

        // 단계 전환 조건은 개별 패턴이 결정한다. 공통 실행기는 단계 순서를 강제하지 않는다.
        protected void ChangePhase(EBossPatternPhase p_phase) => Runtime.SetPhase(p_phase);

        // 공통 거리·쿨다운 외의 추가 조건만 조회한다. 선택과 실행 직전에 재호출된다.
        public abstract bool CanExecute(BossContext p_context);
        public abstract void Enter(BossContext p_context);
        // BossCore의 FixedUpdate에서 전달한 초 단위 시간으로 진행한다.
        public abstract void Update(float p_deltaTime);
        public abstract bool IsFinished { get; }
        // 정상 종료와 중단 모두에서 한 번 호출하는 공통 정리다.
        public virtual void Exit() { }
        // 중단 시에만 Exit보다 먼저 호출한다. 공통 정리는 Exit에 둔다.
        public virtual void Cancel() { }
    }
}
