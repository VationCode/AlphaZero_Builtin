namespace Alpha.Combat
{
    // 피격자의 타입 허용 여부를 확인하고 공격자가 전달한 충격값을 결과로 변환한다.
    public static class ImpactReactionSystem
    {
        public static ImpactReactionResult Resolve(
            in DamageInfo p_damageInfo,
            HitTypeResponseSettings p_responseSettings)
        {
            if (!p_damageInfo.IsValid ||
                p_responseSettings == null ||
                !p_responseSettings.CanRespond(p_damageInfo.HitType))
            {
                return default;
            }

            return Resolve(p_damageInfo.Impact, p_responseSettings);
        }

        public static ImpactReactionResult Resolve(
            in AttackImpactInfo p_impact,
            HitTypeResponseSettings p_responseSettings)
        {
            if (p_responseSettings == null || !p_responseSettings.CanRespond(p_impact.HitType))
                return default;

            return new ImpactReactionResult(
                p_impact.HitType,
                p_impact.RecoveryDuration,
                p_impact.KnockbackDistance,
                p_impact.KnockbackDuration);
        }
    }
}
