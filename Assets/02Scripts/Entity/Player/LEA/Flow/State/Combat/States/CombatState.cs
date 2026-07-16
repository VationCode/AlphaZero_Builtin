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

        if (_normalT <= 0)
        {
            _Core.StateMachineFlow.ChangeCombatState(ECombatType.Normal);
            return;
        }

        if (_Core.BlockCombat) return;

        if (_Core.Input.IsSwapInput && _Core.CombatRule.CanSwap(_Core.Context))
        {
            _Core.EquipmentFlow.TrySelectWeapon(_Core.Input.SwapNum);
            return;
        }


        bool isAim = _Core.Input.IsAim;
        bool isAttack = _Core.Input.IsAttack;

        if (isAim && _Core.CombatRule.CanAim(_Core.Context))
        {
            _Core.StateMachineFlow.ChangeCombatState(ECombatType.Aim);
        }
        else if(isAttack && _Core.CombatRule.CanAttack(_Core.Context))
        {
            _Core.StateMachineFlow.ChangeCombatState(ECombatType.Attack);
        }
    }

    public override void Exit()
    {
 
    }
}
