using UnityEngine;

[CreateAssetMenu(fileName = "Weapon", menuName = "ScriptableObj/Item/Weapon")]
public class WeaponSO : ItemSO
{
    [Space(20)]
    public EWeaponType WeaponType;
    public float BaseDamage;
}
