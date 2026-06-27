using UnityEngine;

public class CombatState : BaseState
{
    private float _normalT;

    public override void Enter()
    {
        base.Enter();
        _Core.Context.IsInCombat = true;

        _normalT = 1.5f;
    }

    public override void Update()
    {
        _normalT -= Time.deltaTime;
        bool isAim = _Core.InputManager.IsAim;
        bool isAttack = _Core.InputManager.IsAttack;


        if (_normalT <= 0)
        {
            _Core.StateMachine.ChangeCombatState(ECombatType.Normal);
        }

        if (_Core.UIManager.IsCombatBlocked) return;

        if (isAim && _Core.CombatRule.CanAim(_Core.Context))
        {
            _Core.StateMachine.ChangeCombatState(ECombatType.Aim);
        }
        else if(isAttack && _Core.CombatRule.CanAttack(_Core.Context))
        {
            _Core.StateMachine.ChangeCombatState(ECombatType.Attack);
        }
    }

    public override void Exit()
    {
 
    }
}
