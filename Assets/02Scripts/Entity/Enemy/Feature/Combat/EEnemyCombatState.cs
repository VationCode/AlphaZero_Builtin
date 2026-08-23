namespace Alpha.Enemy
{
    // Enemy의 공격 준비부터 회복까지의 전투 진행 상태를 구분한다.
    public enum EEnemyCombatState
    {
        Idle = 0,
        Prepare = 1,
        Attack = 2,
        Recovery = 3
    }
}
