namespace Alpha.Enemy
{
    // Enemy 전체 행동의 허용 여부를 결정하는 최상위 상태다.
    public enum EEnemyActionState
    {
        Normal = 0,
        HitReaction = 1,
        Dead = 2
    }
}
