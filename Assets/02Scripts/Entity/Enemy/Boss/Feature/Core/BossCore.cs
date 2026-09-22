using System;
using System.Collections.Generic;
using Alpha.Combat;
using Alpha.Living;
using UnityEngine;

namespace Alpha.Boss
{
    // 내부 상태·행동·이동과 공용 체력 기능을 조립하는 대표 진입점이다.
    [DisallowMultipleComponent, RequireComponent(typeof(Rigidbody))]
    public sealed class BossCore : MonoBehaviour
    {
        [SerializeField, Tooltip("추적에 사용할 Rigidbody입니다. 같은 객체에서 자동으로 찾습니다.")]
        private Rigidbody _body;
        [SerializeField, Tooltip("공용 체력 기능입니다. 비어 있으면 자식에서 찾고, 없으면 실행 시 추가합니다.")]
        private LivingModule _healthModule;
        [SerializeField, Tooltip("공용 피해 수신 기능입니다. 비어 있으면 자식에서 찾고, 없으면 실행 시 추가합니다.")]
        private DamageReceiverModule _damageReceiver;
        [SerializeField, Tooltip("연결하면 시네마틱 완료 후 행동합니다. 같은 보스 아래의 Root는 자동으로 찾습니다.")]
        private BossCinematicRoot _cinematic;
        [SerializeField, Tooltip("감지와 행동 상태 전이 설정입니다.")]
        private BossFlow _flow = new();
        [SerializeField, Tooltip("추적 이동과 회전 설정입니다.")]
        private BossMovementModule _movement = new();
        [SerializeField, Tooltip("보스가 사용할 패턴 설정 에셋입니다. 같은 거리에 여러 타입을 함께 등록할 수 있습니다.")]
        private BossPatternDefinition[] _patternDefinitions = Array.Empty<BossPatternDefinition>();
        [SerializeField, Tooltip("보스 상태와 패턴 단계를 Animator에 표현하는 View입니다.")]
        private BossAnimationView _animationView;

        public BossContext Context { get; } = new();
        public HealthContext HealthContext { get; } = new();
        private readonly BossPatternSelector _selector = new();
        private bool _patternsConfigured;
        private BossPatternFlow _patternFlow;
        public BossPatternExecution? ActivePattern { get; private set; }
        public float ActivePatternElapsedTime => _patternFlow?.ElapsedTime ?? 0f;
        public string SelectedPatternId => _flow.SelectedPatternId;

        // Prepare: 준비·표현, Active: 공격 시작, Recovery/Completed/Cancelled: 정리 연결 지점이다.
        public event Action<BossPatternExecution> OnPatternPhaseChanged;

        private void Awake()
        {
            _body ??= GetComponent<Rigidbody>();
            _healthModule ??= GetComponentInChildren<LivingModule>(true);
            if (_healthModule == null) _healthModule = gameObject.AddComponent<LivingModule>();
            _damageReceiver ??= GetComponentInChildren<DamageReceiverModule>(true);
            if (_damageReceiver == null) _damageReceiver = gameObject.AddComponent<DamageReceiverModule>();
            _cinematic ??= GetComponentInChildren<BossCinematicRoot>(true);
            _healthModule.Bind(HealthContext);
            _damageReceiver.Bind(transform, _healthModule.TryDecreaseHealth);
            _movement.Bind(_body);
            _patternFlow = new BossPatternFlow(Context);
            BossCinematicContext cinematicContext = _cinematic != null ? _cinematic.Context : null;
            _flow.Bind(Context, HealthContext, _movement, cinematicContext, _patternFlow, _selector);
            _animationView ??= GetComponentInChildren<BossAnimationView>(true);
            _animationView?.Bind(this, cinematicContext);
            if (!_patternsConfigured) ConfigurePatterns();
        }
        private void OnEnable() => _flow.SetEnabled(true);
        private void FixedUpdate() => _flow.Tick(Time.fixedDeltaTime);
        private void OnDisable() => _flow.SetEnabled(false);
        private void OnDestroy() { _flow.Unbind(); _animationView?.Unbind(); _damageReceiver?.Unbind(); }

        public bool TryStartPattern(BossPattern p_pattern) => _flow.TryStartPattern(p_pattern);
        // 개별 패턴을 조립한 쪽에서 보스 전용 인스턴스를 전달한다. 선택 확률은 각 설정의 가중치다.
        public void SetPatterns(params BossPattern[] p_patterns)
        {
            _patternsConfigured = true;
            _selector.SetPatterns(p_patterns);
        }
        public void CancelPattern() => _flow.CancelPattern();
        // 경직·그로기 요청의 대표 진입점이다. 지속 시간은 초 단위다.
        public bool TryStagger(float p_duration) => _flow.TryStagger(p_duration);

        // 설정 에셋마다 보스 전용 실행 상태를 가진 객체를 조립한다.
        private void ConfigurePatterns()
        {
            var patterns = new List<BossPattern>();
            var added = new HashSet<BossPatternDefinition>();
            foreach (BossPatternDefinition definition in _patternDefinitions ?? Array.Empty<BossPatternDefinition>())
            {
                if (definition == null || !added.Add(definition)) continue;
                try
                {
                    BossPattern pattern = definition switch
                    {
                        BossMeleePatternDefinition melee => new BossMeleePattern(melee, PublishPatternPhase),
                        BossRushPatternDefinition rush => new BossRushPattern(rush, PublishPatternPhase),
                        BossRangePatternDefinition range => new BossRangePattern(range, PublishPatternPhase),
                        BossGlobalAoEPatternDefinition area => new BossGlobalAoEPattern(area, PublishPatternPhase),
                        _ => throw new ArgumentException("Unsupported boss pattern definition.")
                    };
                    patterns.Add(pattern);
                }
                catch (Exception exception) { Debug.LogException(exception); }
            }
            SetPatterns(patterns.ToArray());
        }

        private void PublishPatternPhase(BossPatternExecution p_execution)
        {
            ActivePattern = p_execution.Phase is EBossPatternPhase.Completed or EBossPatternPhase.Cancelled
                ? null : p_execution;
            Action<BossPatternExecution> handlers = OnPatternPhaseChanged;
            if (handlers == null) return;
            List<Exception> errors = null;
            // 한 수신자의 오류가 다른 Module의 정리 알림을 막지 않도록 모두 호출한다.
            foreach (Action<BossPatternExecution> handler in handlers.GetInvocationList())
            {
                try { handler(p_execution); }
                catch (Exception exception) { (errors ??= new()).Add(exception); }
            }
            if (errors != null) throw new AggregateException(errors);
        }

        private void OnValidate() { _flow.Validate(); _movement.Validate(); }
    }
}
