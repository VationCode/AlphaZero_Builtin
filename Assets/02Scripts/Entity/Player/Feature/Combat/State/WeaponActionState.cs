using UnityEngine;

namespace Alpha.Player.Combat
{
    public class WeaponActionState : CombatStateBase
    {
        public WeaponActionState(PlayerCore p_core, CombatFlow p_flow) : base(p_core){}

        public override ECombatStateType Type => ECombatStateType.WeaponAction;

        protected override void Enter()
        {
            throw new System.NotImplementedException();
        }

        protected override void Exit()
        {
            throw new System.NotImplementedException();
        }

        protected override void Tick()
        {
            throw new System.NotImplementedException();
        }
    }
}