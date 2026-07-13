using UnityEngine;

public class StateContext
{
    public bool IsGroundMove;
    public bool IsSprint;
    public bool IsJump;
    public bool IsDash;
    public bool IsFall;
    public bool IsLand;

    public bool IsAim;
    public bool IsAttack;
    public bool IsInCombat;

    public ELocomotionType? LocomotionType;
    public ECombatType? CombatType;
}
