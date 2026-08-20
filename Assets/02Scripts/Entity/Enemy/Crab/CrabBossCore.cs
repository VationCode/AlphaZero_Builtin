using Alpha.Enemy.CrabBoss.Combat;
using UnityEngine;

namespace Alpha.Enemy.CrabBoss
{
    [RequireComponent(typeof(CrabBossStateMachine))]
    public class CrabBossCore : MonoBehaviour
    {
        [SerializeField] private CrabBossStateMachine _stateMachine;
        [SerializeField] private CrabBossLocomotionModule _locomotionModule;
        [SerializeField] private CrabBossTargetRangeModule _targetRangeModule;
        [SerializeField] private CrabBossAnimationView _animView;
        [SerializeField] private CrabBossCombatModule _combatModule;
        [SerializeField] private DamageCollision _damageCollision;

        public CrabBossStateMachine StateMachine => _stateMachine;
        public CrabBossLocomotionModule LocomotionModule => _locomotionModule;
        public CrabBossTargetRangeModule TargetRangeModule => _targetRangeModule;
        public CrabBossAnimationView AnimView => _animView;
        public CrabBossCombatModule CombatModule => _combatModule;
        public CrabBossContext Context { get; private set; }

        private void Awake()
        {
            _stateMachine ??= GetComponent<CrabBossStateMachine>();
            _locomotionModule ??= GetComponentInChildren<CrabBossLocomotionModule>(true);
            _targetRangeModule ??= GetComponentInChildren<CrabBossTargetRangeModule>(true);
            _animView ??= GetComponentInChildren<CrabBossAnimationView>(true);
            _combatModule ??= GetComponentInChildren<CrabBossCombatModule>(true);
            _damageCollision ??= GetComponentInChildren<DamageCollision>(true);

            Context = new CrabBossContext();
            _stateMachine?.Bind(this);
            _damageCollision?.Bind(transform);
        }

        // BossRoomTrigger가 호출하는 전투 조립 진입점이다.
        public bool BeginEncounter(
            GameObject p_player,
            bool p_skipIntro = false)
        {
            if (p_player == null || Context == null || _stateMachine == null)
                return false;

            Context.SetTarget(p_player.transform);

            return p_skipIntro
                ? CompleteIntro()
                : _stateMachine.ChangeState(CrabState.Intro);
        }

        // 인트로 이후 Idle에서 거리 기반 다음 행동을 결정한다.
        public bool CompleteIntro()
        {
            return _stateMachine != null &&
                   _stateMachine.ChangeState(CrabState.Idle);
        }

        public void StartCombat()
        {
            CompleteIntro();
        }
    }
}
