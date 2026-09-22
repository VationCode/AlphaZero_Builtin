using Unity.Cinemachine;
using UnityEngine;

namespace Alpha.Boss
{
    // Boss가 소유한 Cinematic Feature를 조립하고 Scene 의존성을 받는다.
    [DisallowMultipleComponent]
    public sealed class BossCinematicRoot : MonoBehaviour
    {
        [Header("Entity")]
        [SerializeField]
        private Animator _bossAnimator;

        [SerializeField, Tooltip("시네마틱 카메라가 바라볼 보스 Transform입니다.")]
        private Transform _bossTarget;

        [Header("Feature")]
        [SerializeField]
        private BossCinematicFlow _flow;

        [Header("View")]
        [SerializeField]
        private BossCinematicView _cinematicView;

        [SerializeField]
        private BossCinematicTriggerView _triggerView;

        public BossCinematicFlow Flow => _flow;
        // Core의 Awake 순서와 관계없이 상태를 연결할 수 있게 대표 진입점에서 해석한다.
        public BossCinematicContext Context
        {
            get { ResolveOwnedReferences(); return _flow != null ? _flow.Context : null; }
        }

        private void Awake()
        {
            if (!Initialize())
            {
                Debug.LogError(
                    "Boss Cinematic 참조가 완전하지 않아 초기화할 수 없습니다.",
                    this);
            }
        }

        // Animator·View → Flow → Trigger 순서로 내부 참조를 조립한다.
        public bool Initialize()
        {
            ResolveOwnedReferences();

            if (_bossAnimator == null ||
                _bossTarget == null ||
                _flow == null ||
                _cinematicView == null ||
                _triggerView == null ||
                !_cinematicView.BindBossAnimation(
                    _bossAnimator,
                    _bossTarget))
            {
                return false;
            }

            return _flow.Bind(
                       _cinematicView) &&
                   _triggerView.Bind(_flow);
        }

        public bool BindInput(AlphaInputSystem p_input)
        {
            ResolveOwnedReferences();
            return _flow != null && _flow.BindInput(p_input);
        }

        public bool BindCamera(CinemachineBrain p_brain)
        {
            ResolveOwnedReferences();

            bool didBind =
                _cinematicView != null &&
                _cinematicView.BindCamera(p_brain);
            _flow?.RefreshCinematicTrigger();
            return didBind;
        }

        private void ResolveOwnedReferences()
        {
            _flow ??= GetComponent<BossCinematicFlow>();
            _cinematicView ??= GetComponent<BossCinematicView>();
            _triggerView ??=
                GetComponentInChildren<BossCinematicTriggerView>(true);
        }

        private void OnValidate()
        {
            ResolveOwnedReferences();
        }
    }
}
