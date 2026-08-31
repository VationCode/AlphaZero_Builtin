namespace Alpha.Enemy
{
    // Enemy가 현재 수행하는 일반 이동 행동을 구분한다.
    public enum EEnemyLocomotionState
    {
        Idle = 0,
        Patrol = 1,
        Chase = 2,
        ReturnToArea = 3,
        Retreat = 4
    }
}
