namespace Alpha.Combat
{
    // 넉백 가능 여부와 실제 이동 처리를 대상 Entity가 결정하는 계약이다.
    public interface IKnockbackable
    {
        bool CanReceiveKnockback { get; }
        bool TryApplyKnockback(in KnockbackInfo p_knockbackInfo);
    }
}
