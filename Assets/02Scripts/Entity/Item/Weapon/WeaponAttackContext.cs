using UnityEngine;

// 한 번의 무기 공격 실행에 필요한 외부 정보를 보관한다.
public readonly struct WeaponAttackContext
{
    public GameObject Instigator { get; }               // 공격을 실행한 Player·Enemy 식별
    public Vector3 AttackDirection { get; }             // 공격 실행 시점의 월드 방향
    public LayerMask HitMask { get; }


    public bool IsValid =>                              // 무기가 실행 전에 요청 유효성을 검사할 수 있도록 제공
        Instigator != null && AttackDirection.sqrMagnitude > 0.0001f &&
        HitMask.value != 0;

    public WeaponAttackContext(GameObject p_instigator, Vector3 p_attackDirection, LayerMask p_hitMask)
    {
        Instigator = p_instigator;

        AttackDirection =
            p_attackDirection.sqrMagnitude > 0.0001f? p_attackDirection.normalized : Vector3.zero;

        HitMask = p_hitMask;
    }
}
