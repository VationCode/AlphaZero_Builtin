namespace Alpha.Player.Equipment
{
    // Player가 현재 장착하여 사용하는 무기 상태 보관
    public class PlayerEquipmentContext
    {
        public WeaponDTO CurrentWeapon { get; private set; }

        public EWeaponType CurrentWeaponType =>
            CurrentWeapon?.WeaponType ?? EWeaponType.None;

        public bool HasWeapon => CurrentWeapon != null;

        // 무기가 실제로 변경됐을 때만 true를 반환한다.
        internal bool TrySetWeapon(WeaponDTO p_weapon)
        {
            if (p_weapon == null)
                return false;

            if (CurrentWeapon != null && CurrentWeapon.Id == p_weapon.Id)
            {
                return false;
            }

            CurrentWeapon = p_weapon;
            return true;
        }

        internal bool TryClearWeapon()
        {
            if (CurrentWeapon == null)
                return false;

            CurrentWeapon = null;
            return true;
        }
    }
}
