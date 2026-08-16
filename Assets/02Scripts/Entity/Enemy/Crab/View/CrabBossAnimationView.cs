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

        [Header("Attack Animation Clips")]
        [SerializeField] private CrabBossAttackAnimationSetting[] _meleeAttackClips;
        [SerializeField] private CrabBossAttackAnimationSetting[] _rangeAttackClips;
        [SerializeField] private CrabBossAttackAnimationSetting[] _rushAttackClips;
        [SerializeField] private CrabBossAttackAnimationSetting[] _areaAttackClips;
        [SerializeField] private CrabBossAttackAnimationSetting[] _arenaAttackClips;

        private bool _isRootMotionEnabled;

        public Animator Animator => _anim;

        private void Awake()
        {
            // OnAnimatorMove를 받기 위해 같은 GameObject의 Animator를 사용한다.
            _anim = GetComponent<Animator>();

            if (_root == null)
            {
                CrabBossCore core = GetComponentInParent<CrabBossCore>();
                _root = core != null ? core.transform : transform.root;
            }
        }

        // 인트로 1 시작만 처리
        public bool PlayIntro()
        {
            DisableRootMotion();

            if (_anim == null || !_anim.HasState(0, Intro1Hash))
            {
                Debug.LogWarning("Crab Boss Animator의 Base Layer에 Intro1 State가 필요합니다.", this);

                return false;
            }

            // 이후 애니메이션과 Idle 전환은 Animator가 담당한다.
            _anim.Play(Intro1Hash, 0, 0f);

            return true;
        }

        public CrabBossAttackAnimationSetting PlayRandomAttack(
            ECrabAttackPattern p_pattern)
        {
            CrabBossAttackAnimationSetting[] settings =
                GetAttackSettings(p_pattern);

            if (_anim == null || settings == null || settings.Length == 0)
                return null;

            int startIndex = Random.Range(0, settings.Length);

            for (int offset = 0; offset < settings.Length; offset++)
            {
                int index = (startIndex + offset) % settings.Length;
                CrabBossAttackAnimationSetting selected = settings[index];

                if (selected == null || selected.Clip == null)
                    continue;

                int stateHash = Animator.StringToHash(
                    $"Base Layer.{selected.Clip.name}");

                if (!_anim.HasState(0, stateHash))
                    continue;

                _isRootMotionEnabled = selected.UseRootMotion;
                _anim.Play(stateHash, 0, 0f);
                return selected;
            }

            DisableRootMotion();
            Debug.LogWarning($"{p_pattern}에 재생 가능한 Animator State가 없습니다.", this);

            return null;
        }

        public void DisableRootMotion()
        {
            _isRootMotionEnabled = false;
        }

        private void OnAnimatorMove()
        {
            if (!_isRootMotionEnabled || _anim == null || _root == null)
                return;

            // Animator 자식이 아닌 CrabBoss 루트에 RootMotion을 적용한다.
            _root.position += _anim.deltaPosition;
            _root.rotation *= _anim.deltaRotation;
        }

        private void OnDisable()
        {
            DisableRootMotion();
        }

        private CrabBossAttackAnimationSetting[] GetAttackSettings(
            ECrabAttackPattern p_pattern)
        {
            return p_pattern switch
            {
                ECrabAttackPattern.MeleeAttack => _meleeAttackClips,
                ECrabAttackPattern.RangeAttack => _rangeAttackClips,
                ECrabAttackPattern.RushAttack => _rushAttackClips,
                ECrabAttackPattern.AreaAttack => _areaAttackClips,
                ECrabAttackPattern.ArenaAttack => _arenaAttackClips,
                _ => null
            };
        }
    }
}
