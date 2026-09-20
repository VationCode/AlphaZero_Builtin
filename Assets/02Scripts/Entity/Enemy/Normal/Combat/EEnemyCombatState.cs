namespace Alpha.Enemy
{
    // Enemy의 타겟 확인, 공격 애니메이션과 다음 공격 대기를 구분한다.
    public enum EEnemyCombatState
    {
        Idle = 0,
        Prepare = 1,
        Attack = 2,
        Wait = 3
    }
}
