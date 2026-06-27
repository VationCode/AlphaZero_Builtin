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
        bool isAining = _Core.InputManager.IsAim;
        if (_Core.UIManager.IsCombatBlocked) isAining = false;

        if (!isAining) _Core.StateMachine.ChangeCombatState(ECombatType.Combat);
    }

    public override void Exit()
    {
        _Core.Context.IsAim = false;
        _Core.CombatModule.Aim(false);
    }
}
