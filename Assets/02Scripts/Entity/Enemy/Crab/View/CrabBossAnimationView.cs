using Alpha.Enemy.CrabBoss.Combat;
using UnityEngine;

namespace Alpha.Enemy.CrabBoss
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public class CrabBossAnimationView : MonoBehaviour
    {
        private static readonly int Intro1Hash = Animator.StringToHash("Base Layer.Intro1");

        [SerializeField] private Animator _anim;
        [SerializeField] private Transform _root;
        [SerializeField] private CrabBossCore _core;

        [SerializeField]
        private CrabBossAttackAnimationSetting[] _attackAnimations;

        public bool IsRootMotionEnabled { get; private set; }

        private static readonly int IdleHash =
            Animator.StringToHash("Base Layer.Idle");

        private static readonly int WalkHash =
            Animator.StringToHash("Base Layer.Walk_F");

        private void Awake()
        {
            // OnAnimatorMove를 받기 위해 같은 GameObject의 Animator를 사용한다.
            _anim = GetComponent<Animator>();

            if (_root == null)
            {
                _core ??= GetComponentInParent<CrabBossCore>();
                _root = _core != null ? _core.transform : transform.root;
            }

            _core ??= GetComponentInParent<CrabBossCore>();
        }

        // 인트로 1 시작만 처리
        public bool PlayIntro()
        {
            if (_anim == null || !_anim.HasState(0, Intro1Hash))
            {
                Debug.LogWarning("Crab Boss Animator의 Base Layer에 Intro1 State가 필요합니다.", this);

                return false;
            }

            // 이후 애니메이션과 Idle 전환은 Animator가 담당한다.
            _anim.Play(Intro1Hash, 0, 0f);

            return true;
        }

        public void PlayIdle()
        {
            SetRootMotionEnabled(false);
            _anim?.CrossFadeInFixedTime(IdleHash, 0.15f);
        }

        public void PlayWalk()
        {
            SetRootMotionEnabled(false);
            _anim?.CrossFadeInFixedTime(WalkHash, 0.15f);
        }

        public int GetAttackAnimationCount(
            EAttackPattern p_pattern)
        {
            CrabBossAttackAnimationSetting setting =
                FindAttackAnimationSetting(p_pattern);

            return setting?.StateCount ?? 0;
        }

        public bool PlayAttack(
            EAttackPattern p_pattern,
            int p_animationIndex)
        {
            if (!TryGetAttackStateHash(
                    p_pattern,
                    p_animationIndex,
                    out int stateHash,
                    out string stateName))
            {
                return false;
            }

            if (_anim == null || !_anim.HasState(0, stateHash))
            {
                Debug.LogWarning(
                    $"Crab Boss Animator의 Base Layer에 {stateName} State가 필요합니다.",
                    this);
                return false;
            }

            // Rush 이동은 CombatStrategy가 처리하므로 공격 RootMotion은 사용하지 않는다.
            SetRootMotionEnabled(false);
            _anim.CrossFadeInFixedTime(stateHash, 0.1f, 0, 0f);

            return true;
        }

        public bool IsAttackComplete(
            EAttackPattern p_pattern,
            int p_animationIndex)
        {
            if (_anim == null ||
                _anim.IsInTransition(0) ||
                !TryGetAttackStateHash(
                    p_pattern,
                    p_animationIndex,
                    out int stateHash,
                    out _))
            {
                return false;
            }

            AnimatorStateInfo stateInfo =
                _anim.GetCurrentAnimatorStateInfo(0);

            return stateInfo.fullPathHash == stateHash &&
                   stateInfo.normalizedTime >= 1f;
        }

        private CrabBossAttackAnimationSetting FindAttackAnimationSetting(
            EAttackPattern p_pattern)
        {
            if (_attackAnimations == null)
                return null;

            foreach (CrabBossAttackAnimationSetting setting in _attackAnimations)
            {
                if (setting != null && setting.Pattern == p_pattern)
                    return setting;
            }

            return null;
        }

        private bool TryGetAttackStateHash(
            EAttackPattern p_pattern,
            int p_animationIndex,
            out int p_stateHash,
            out string p_stateName)
        {
            p_stateHash = 0;
            p_stateName = null;

            CrabBossAttackAnimationSetting setting =
                FindAttackAnimationSetting(p_pattern);

            if (setting == null ||
                !setting.TryGetStateName(
                    p_animationIndex,
                    out p_stateName))
            {
                return false;
            }

            p_stateHash = Animator.StringToHash(
                $"Base Layer.{p_stateName}");

            return true;
        }

        public void SetRootMotionEnabled(bool p_enabled)
        {
            IsRootMotionEnabled = p_enabled;
        }

        private void OnAnimatorMove()
        {
            if (!IsRootMotionEnabled || _anim == null || _root == null)
                return;

            // Animator 자식이 아닌 CrabBoss 루트에 RootMotion을 적용한다.
            _root.position += _anim.deltaPosition;
            _root.rotation *= _anim.deltaRotation;
        }

        private void OnDisable()
        {
            SetRootMotionEnabled(false);
        }
    }
}
