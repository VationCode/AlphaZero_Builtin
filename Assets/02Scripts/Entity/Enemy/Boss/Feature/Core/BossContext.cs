using System;
using UnityEngine;

namespace Alpha.Boss
{
    // 기존 상태의 숫자 값을 유지해 Unity 직렬화 참조를 보존한다.
    public enum EBossState { Idle = 0, Chase = 1, ExecutePattern = 2, Dead = 3, Stagger = 4 }

    // 보스의 현재 행동·타겟·이동 여부만 보관한다. 상태 전이 판단은 BossFlow가 소유한다.
    public sealed class BossContext
    {
        public EBossState State { get; private set; } = EBossState.Idle;
        public Transform Target { get; private set; }
        public bool IsMoving { get; private set; }
        // 활성화된 보스의 FixedUpdate 누적 시간이다. 패턴별 쿨다운의 공통 기준이다.
        public double ElapsedTime { get; private set; }
        public event Action<EBossState> OnStateChanged;

        internal void SetTarget(Transform p_target) => Target = p_target;
        internal void SetMoving(bool p_isMoving) => IsMoving = p_isMoving;
        internal void AdvanceTime(float p_deltaTime) => ElapsedTime += p_deltaTime;

        internal void ChangeState(EBossState p_state)
        {
            if (State == p_state)
                return;
            State = p_state;
            OnStateChanged?.Invoke(p_state);
        }
    }
}
