namespace Alpha.Boss
{
    // 거리 대응 패턴과 독립적인 광역 패턴을 분류한다.
    public enum EBossPatternGroupType
    {
        Near = 0,
        // 기존 Scene의 저장값을 유지한다. 이전 Middle(1)은 Far로 변환한다.
        Far = 2,
        AoE = 3
    }
}
