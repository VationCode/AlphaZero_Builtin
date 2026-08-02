using UnityEngine;

public class Gun : Weapon
{
    [Header("Fire")]
    [SerializeField] private Transform _firePoint;      // 탄환이 시작될 월드 위치

    public RangeWeaponDTO GunData { get; private set; }

    public Transform FirePoint => _firePoint;
   
    private IGunFireStrategy _fireStrategy;
    
    protected override bool CanInitialize(WeaponDTO p_data)
    {
        return p_data is RangeWeaponDTO && _firePoint != null;
    }

    protected override void OnInitialized()
    {
        GunData = (RangeWeaponDTO)Data;
    }

    // 전략 존재 확인
    protected override bool TryExecuteAttack(in WeaponAttackContext p_context)
    {
        if (_fireStrategy == null) return false;

        return _fireStrategy.TryFire(this, p_context);
    }
}
