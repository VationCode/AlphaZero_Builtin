namespace Alpha.Enemy
{
    // Enemy가 현재 수행할 대표 행동을 구분한다.
    public enum EEnemyActionState
    {
        Patrol = 0,
        Chase = 1,
        Attack = 2,
        ReturnToPatrol = 3,
        HitReaction = 4
    }
}
