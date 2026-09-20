using Alpha.Player;
using UnityEngine;

namespace Alpha.Boss
{
    // Player의 Unity Trigger 진입만 감지해 BossCinematicFlow에 전달한다.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class BossCinematicTriggerView : MonoBehaviour
    {
        private BossCinematicFlow _flow;

        private Collider _trigger;
        private bool _isSubscribed;

        private void Awake()
        {
            ResolveTrigger();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        // Root가 연결한 Flow만 구독해 Scene 계층에 의존하지 않는다.
        public bool Bind(BossCinematicFlow p_flow)
        {
            if (p_flow == null)
                return false;

            Unsubscribe();
            _flow = p_flow;
            Subscribe();
            return true;
        }

        private void Subscribe()
        {
            if (_isSubscribed ||
                _flow == null ||
                !isActiveAndEnabled)
            {
                return;
            }

            _flow.OnCinematicTriggerArmedChanged += SetArmed;
            _isSubscribed = true;
            SetArmed(_flow.IsCinematicTriggerArmed);
        }

        private void Unsubscribe()
        {
            if (_isSubscribed && _flow != null)
                _flow.OnCinematicTriggerArmedChanged -= SetArmed;

            _isSubscribed = false;
        }

        private void OnTriggerEnter(Collider p_other)
        {
            TryStartCinematic(p_other);
        }

        // 준비 직후 이미 Trigger 안에 있는 Player도 다음 물리 갱신에서 처리한다.
        private void OnTriggerStay(Collider p_other)
        {
            TryStartCinematic(p_other);
        }

        private void TryStartCinematic(Collider p_other)
        {
            if (_flow == null ||
                p_other.GetComponentInParent<PlayerCore>() is not
                    PlayerCore player)
            {
                return;
            }

            _flow.RequestStart(player);
        }

        private void SetArmed(bool p_isArmed)
        {
            _trigger ??= GetComponent<Collider>();

            if (_trigger != null)
                _trigger.enabled = p_isArmed;
        }

        private void ResolveTrigger()
        {
            _trigger ??= GetComponent<Collider>();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnValidate()
        {
            ResolveTrigger();

            if (_trigger != null)
                _trigger.isTrigger = true;
        }
    }
}
