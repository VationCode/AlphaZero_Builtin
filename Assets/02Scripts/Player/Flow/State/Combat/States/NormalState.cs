using UnityEngine;

public class NormalState : BaseState
{
    public override void Enter()
    {
        base.Enter();
        _Core.Context.IsInCombat = false;
    }

    public override void Update()
    {
        if (_Core.UIManager.IsCombatBlocked) return;

        bool isAim = _Core.InputManager.IsAim;
        bool isAttack = _Core.InputManager.IsAttack;
        bool isInCombat = isAim || isAttack;

        if (isInCombat && _Core.CombatRule.CanInCombat(_Core.Context))
        {
            _Core.StateMachine.ChangeCombatState(ECombatType.Combat);
        }
    }

    public override void Exit()
    {
        
    }
}
