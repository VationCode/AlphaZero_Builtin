// Gun이 실제 탄환을 전달하는 방식을 정의한다.
public interface IGunFireStrategy
{
    bool TryFire(Gun p_gun, in WeaponAttackContext p_context);
}
