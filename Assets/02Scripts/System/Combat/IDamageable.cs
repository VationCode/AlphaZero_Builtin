namespace Alpha.Combat
{
    // 피해를 받을 수 있는 Entity가 구현하는 공용 계약이다.
    public interface IDamageable
    {
        bool TryApplyDamage(DamageInfo p_damageInfo);
    }
}
