namespace Alpha.Boss
{
    // 실제 공격 종류다. 거리 조건과 이동 기능의 사용 여부는 별도로 판단한다.
    public enum EBossAttackType
    {
        Melee = 0,
        Range = 1,
        Rush = 2,
        Area = 3,
        Arena = 4
    }
}
