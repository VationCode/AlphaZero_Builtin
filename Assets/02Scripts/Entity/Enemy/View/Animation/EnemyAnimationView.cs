using System;
using UnityEngine;

namespace Alpha.Enemy.Animation
{
    // Enemy의 피격·사망 등 Animator 표현을 담당한다.
    public class EnemyAnimationView : MonoBehaviour
    {
        [SerializeField]
        private Animator _animator;

        [Header("Attack")]
        [SerializeField]
        private string _attackTriggerParameter = "Attack";

        [SerializeField]
        private string _attackIndexParameter = "AttackIndex";

        private static readonly int Hit =
            Animator.StringToHash("Hit");

        private static readonly int Die =
            Animator.StringToHash("Die");

        private int _attackTrigger;
        private int _attackIndex;
        private bool _hasAttackTrigger;
        private bool _hasAttackIndex;

        public event Action OnDeathAnimationCompleted;

        private void Awake()
        {
            _animator ??=
                GetComponentInChildren<Animator>(true);

            CacheAttackParameters();
        }

        // 패턴별 Index를 전달하고 공통 Attack Trigger를 실행한다.
        public bool PlayAttack(int p_animationIndex)
        {
            if (_animator == null || !_hasAttackTrigger)
                return false;

            if (_hasAttackIndex && p_animationIndex >= 0)
                _animator.SetInteger(_attackIndex, p_animationIndex);

            _animator.SetTrigger(_attackTrigger);
            return true;
        }

        public bool PlayHit()
        {
            if (_animator == null)
                return false;

            _animator.SetTrigger(Hit);
            return true;
        }

        public bool PlayDeath()
        {
            if (_animator == null)
                return false;

            // 피격 표현보다 사망 표현을 우선한다.
            _animator.ResetTrigger(Hit);
            _animator.SetTrigger(Die);

            return true;
        }

        // Death Animation Clip 마지막 프레임의 Animation Event에서 호출한다.
        public void NotifyDeathAnimationCompleted()
        {
            OnDeathAnimationCompleted?.Invoke();
        }

        private void CacheAttackParameters()
        {
            _attackTrigger = Animator.StringToHash(
                _attackTriggerParameter ?? string.Empty);
            _attackIndex = Animator.StringToHash(
                _attackIndexParameter ?? string.Empty);

            _hasAttackTrigger = HasParameter(
                _attackTrigger,
                AnimatorControllerParameterType.Trigger);
            _hasAttackIndex = HasParameter(
                _attackIndex,
                AnimatorControllerParameterType.Int);
        }

        private bool HasParameter(
            int p_nameHash,
            AnimatorControllerParameterType p_type)
        {
            if (_animator == null)
                return false;

            foreach (AnimatorControllerParameter parameter in
                     _animator.parameters)
            {
                if (parameter.nameHash == p_nameHash &&
                    parameter.type == p_type)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
