using UnityEngine;

public class AttackState : BaseState
{
    public override void Enter()
    {
        base.Enter();
        _Core.Context.IsAttack = true;
    }

    public override void Update()
    {
        bool isAttack = _Core.Input.IsAttack;

        if (_Core.BlockCombat) isAttack = false;

        if (!isAttack) _Core.StateMachineFlow.ChangeCombatState(ECombatType.Combat);
    }

    public override void Exit()
    {
        _Core.Context.IsAttack = false;
    }
}
