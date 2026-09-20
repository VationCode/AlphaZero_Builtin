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
        private BossCore _boss;

        [Header("Feature")]
        [SerializeField]
        private BossCinematicModule _cinematicModule;

        [SerializeField]
        private BossCinematicFlow _flow;

        [Header("View")]
        [SerializeField]
        private BossCinematicView _cinematicView;

        [SerializeField]
        private BossCinematicTriggerView _triggerView;

        public BossCinematicFlow Flow => _flow;

        private void Awake()
        {
            if (!Initialize())
            {
                Debug.LogError(
                    "Boss Cinematic 참조가 완전하지 않아 초기화할 수 없습니다.",
                    this);
            }
        }

        // Boss → Module·View → Flow → Trigger 순서로 내부 참조를 조립한다.
        public bool Initialize()
        {
            ResolveOwnedReferences();

            if (_boss == null ||
                _cinematicModule == null ||
                _flow == null ||
                _cinematicView == null ||
                _triggerView == null ||
                !_cinematicModule.Bind(_boss) ||
                !_cinematicView.BindBossAnimation(
                    _boss.AnimationView,
                    _boss.transform))
            {
                return false;
            }

            return _flow.Bind(
                       _boss,
                       _cinematicModule,
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
            if (_boss == null && transform.parent != null)
            {
                _boss = transform.parent
                    .GetComponentInChildren<BossCore>(true);
            }

            _cinematicModule ??=
                GetComponent<BossCinematicModule>();
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
