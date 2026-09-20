using System;

namespace Alpha.Boss
{
    public enum EBossCombatState { Idle, Attack, Recovery }

    // 현재 공격과 실행 순서를 보관한다. 발사 시점 판단은 CombatFlow가 담당한다.
    public sealed class BossCombatContext
    {
        private int[] _groupOrder = Array.Empty<int>();
        private float[] _fireTimes = Array.Empty<float>();

        public BossPatternData CurrentPattern { get; private set; }
        public long AttackId { get; private set; }
        public EBossCombatState State { get; private set; }
        public int NextGroup { get; private set; }
        public int GroupCount => _groupOrder.Length;
        public float ElapsedSeconds { get; internal set; }
        // Rush·Area의 게임 시간과 분리한 애니메이션 피해 판정 시간이다.
        public float AnimationElapsedSeconds { get; internal set; }
        public float RecoveryRemaining { get; internal set; }
        public bool AnimationStarted { get; internal set; }
        public bool AnimationCompleted { get; internal set; }
        public BossRushAttackContext Rush { get; } = new();
        public BossDamageContext Damage { get; } = new();

        internal void Begin(BossPatternData p_pattern)
        {
            AttackId++;
            CurrentPattern = p_pattern;
            State = EBossCombatState.Attack;
            NextGroup = 0;
            ElapsedSeconds = 0f;
            AnimationElapsedSeconds = 0f;
            RecoveryRemaining = 0f;
            AnimationStarted = false;
            AnimationCompleted = false;
            Rush.Clear();
            Damage.Clear();
            int count = p_pattern.AttackType switch
            {
                EBossAttackType.Area => p_pattern.AreaAttack.RepeatCount,
                EBossAttackType.Arena => p_pattern.ArenaAttack.SpawnGroupCount,
                EBossAttackType.Range => p_pattern.RangeAttack.FireGroupCount,
                _ => 0
            };
            _groupOrder = new int[count];
            _fireTimes = new float[count];
            for (int index = 0; index < count; index++)
            {
                _groupOrder[index] = index;
                _fireTimes[index] = p_pattern.AttackType switch
                {
                    EBossAttackType.Area => p_pattern.AreaAttack.GetSpawnTimeSeconds(index),
                    EBossAttackType.Arena => p_pattern.ArenaAttack.GetSpawnGroup(index).SpawnTimeSeconds,
                    _ => p_pattern.RangeAttack.GetFireGroup(index).FireTimeSeconds
                };
            }
            // Inspector 순서를 바꾸지 않고 실행용 순서만 만든다. 같은 시간은 등록 순서를 유지한다.
            Array.Sort(_groupOrder, (left, right) =>
            {
                int timeOrder = _fireTimes[left].CompareTo(_fireTimes[right]);
                return timeOrder != 0 ? timeOrder : left.CompareTo(right);
            });
        }

        internal float NextFireTime => _fireTimes[_groupOrder[NextGroup]];
        internal int ConsumeGroup() => _groupOrder[NextGroup++];

        internal void BeginRecovery(float p_seconds)
        {
            State = EBossCombatState.Recovery;
            RecoveryRemaining = p_seconds;
        }

        internal void Clear()
        {
            CurrentPattern = null;
            State = EBossCombatState.Idle;
            NextGroup = 0;
            ElapsedSeconds = 0f;
            AnimationElapsedSeconds = 0f;
            RecoveryRemaining = 0f;
            AnimationStarted = false;
            AnimationCompleted = false;
            Rush.Clear();
            Damage.Clear();
            _groupOrder = Array.Empty<int>();
            _fireTimes = Array.Empty<float>();
        }
    }
}
