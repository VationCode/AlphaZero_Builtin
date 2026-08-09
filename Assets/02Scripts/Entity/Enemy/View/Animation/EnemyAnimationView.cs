using System;
using UnityEngine;

namespace Alpha.Enemy.Animation
{
    // Enemy의 피격·사망 등 Animator 표현을 담당한다.
    public class EnemyAnimationView : MonoBehaviour
    {
        [SerializeField]
        private Animator _animator;

        private static readonly int Hit =
            Animator.StringToHash("Hit");

        private static readonly int Die =
            Animator.StringToHash("Die");

        public event Action OnDeathAnimationCompleted;

        private void Awake()
        {
            _animator ??=
                GetComponentInChildren<Animator>(true);
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
    }
}