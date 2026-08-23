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

            EHitReaction reaction = ResolveReaction(p_damageInfo.HitType);

            if (reaction == EHitReaction.None)
                return default;

            AttackImpactInfo impact = p_damageInfo.Impact;

            return new ImpactReactionResult(
                reaction,
                impact.RecoveryDuration,
                impact.KnockbackDistance,
                impact.KnockbackDuration);
        }

        private static EHitReaction ResolveReaction(EHitType p_hitType)
        {
            return p_hitType switch
            {
                EHitType.Light => EHitReaction.Light,
                EHitType.Heavy => EHitReaction.Heavy,
                EHitType.Knockdown => EHitReaction.Knockdown,
                EHitType.Launch => EHitReaction.Launch,
                _ => EHitReaction.None
            };
        }
    }
}
