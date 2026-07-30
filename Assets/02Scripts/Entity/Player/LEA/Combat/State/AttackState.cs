
namespace Alpha.Player.Combat
{
    // 현재 조준 방향과 Player 방향의 정렬 여부를 판단한다.
    public class AttackState : CombatStateBase
    {
        public AttackState(PlayerCore p_core, CombatFlow p_flow): base(p_core, p_flow) { }
        public override ECombatStateType Type => ECombatStateType.Attack;

        private const float FacingToleranceAngle = 5f;

        protected override void Enter()
        {

        }

        protected override void Tick()
        {
            if (_Input == null || !_Input.IsAttack || _Core.BlockCombat)
            {
                TryChangeState(ECombatStateType.Idle);

                return;
            }

            if (!_Context.HasAimDirection)
                return;

            // 카메라나 마우스가 움직이면 변경된 방향을 기준으로 다시 검사한다.
            if (!_Core.LocomotionModule.IsFacingDirection(_Context.AimDirection, FacingToleranceAngle))
            {
                return;
            }

            // 현재 프레임에는 조준 방향과 Player 방향이 정렬되어 있다.
            // 실제 총알 발사 또는 근접 공격 실행 위치
        }

        protected override void Exit()
        {
            // 현재 공격만 정리
            _Context.ClearActiveAttack();
        }
    }
}
