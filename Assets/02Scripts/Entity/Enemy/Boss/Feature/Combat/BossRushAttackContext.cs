using UnityEngine;

namespace Alpha.Boss
{
    // 한 번의 Rush가 확정한 경로와 이동 진행 상태를 보관한다.
    public sealed class BossRushAttackContext
    {
        public bool Started { get; internal set; }
        public bool Completed { get; internal set; }
        public Vector3 StartPosition { get; internal set; }
        public Vector3 Direction { get; internal set; }
        public float ElapsedSeconds { get; internal set; }
        internal AnimationCurve RootMotionCurve { get; set; }
        internal Vector3 PreviousOffset { get; set; }

        internal void Clear()
        {
            Started = false;
            Completed = false;
            StartPosition = Vector3.zero;
            Direction = Vector3.zero;
            ElapsedSeconds = 0f;
            RootMotionCurve = null;
            PreviousOffset = Vector3.zero;
        }
    }
}
