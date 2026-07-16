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
        if (_Core.BlockCombat) return;

        if (_Core.Input.IsSwapInput && _Core.CombatRule.CanSwap(_Core.Context))
        {
            _Core.EquipmentFlow.TrySelectWeapon(
                _Core.Input.SwapNum);

            // 같은 프레임에 공격이나 조준까지 처리하지 않는다.
            return;
        }

        bool isAim = _Core.Input.IsAim;
        bool isAttack = _Core.Input.IsAttack;
        bool isInCombat = isAim || isAttack;

        if (isInCombat && _Core.CombatRule.CanInCombat(_Core.Context))
        {
            _Core.StateMachineFlow.ChangeCombatState(ECombatType.Combat);
        }
    }

    public override void Exit()
    {
        
    }
}
