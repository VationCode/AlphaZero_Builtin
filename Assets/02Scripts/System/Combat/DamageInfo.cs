
using UnityEngine;

public readonly struct DamageInfo
{
    public float Amount { get; }
    public GameObject Instigator { get; }
    public Vector3 HitPoint { get; }
    public Vector3 Direction { get; }

    public DamageInfo(float p_amount, GameObject p_instigator, Vector3 p_hitPoint, Vector3 p_direction)
    {
        Amount = p_amount;
        Instigator = p_instigator;
        HitPoint = p_hitPoint;
        Direction = p_direction;
    }
}
