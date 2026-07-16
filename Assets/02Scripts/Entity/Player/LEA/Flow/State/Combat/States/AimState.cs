using UnityEngine;

public class AimState : BaseState
{
    public override void Enter()
    {
        base.Enter();
        _Core.Context.IsAim = true;

        _Core.CombatModule.Aim(true);
    }

    public override void Update()
    {
        bool isAiming = _Core.Input.IsAim;

        if (_Core.BlockCombat) isAiming = false;

        if (!isAiming) _Core.StateMachineFlow.ChangeCombatState(ECombatType.Combat);
    }

    public override void Exit()
    {
        _Core.Context.IsAim = false;
        _Core.CombatModule.Aim(false);
    }
}
