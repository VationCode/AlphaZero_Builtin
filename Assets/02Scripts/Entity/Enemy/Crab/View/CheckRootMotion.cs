using UnityEngine;

namespace Alpha.Enemy.CrabBoss
{
    public sealed class CheckRootMotion : StateMachineBehaviour
    {
        private CrabBossAnimationView _animView;

        public override void OnStateEnter(
            Animator p_animator,
            AnimatorStateInfo p_stateInfo,
            int p_layerIndex)
        {
            _animView =
                p_animator.GetComponent<CrabBossAnimationView>();
            _animView?.SetRootMotionEnabled(true);
        }

        public override void OnStateExit(
            Animator p_animator,
            AnimatorStateInfo p_stateInfo,
            int p_layerIndex)
        {
            _animView?.SetRootMotionEnabled(false);
            _animView = null;
        }
    }
}
