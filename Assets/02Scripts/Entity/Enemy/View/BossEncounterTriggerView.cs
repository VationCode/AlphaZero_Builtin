using Alpha.Player;
using UnityEngine;

namespace Alpha.Enemy.View
{
    // Boss Room 진입을 Unity Trigger로 감지해 Encounter Flow에 전달한다.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class BossEncounterTriggerView : MonoBehaviour
    {
        [SerializeField]
        private CrabBossEncounterFlow _encounterFlow;

        private Collider _trigger;

        private void Awake()
        {
            _trigger = GetComponent<Collider>();
        }

        private void OnEnable()
        {
            if (_encounterFlow == null)
                return;

            _encounterFlow.OnIntroTriggerArmedChanged -= SetArmed;
            _encounterFlow.OnIntroTriggerArmedChanged += SetArmed;
            SetArmed(_encounterFlow.IsIntroTriggerArmed);
        }

        private void OnTriggerEnter(Collider p_other)
        {
            if (_encounterFlow == null ||
                p_other.GetComponentInParent<PlayerCore>() is not
                    PlayerCore player)
            {
                return;
            }

            // 인트로 시작에 성공한 Trigger는 다시 진입하지 못하도록 전체 비활성화한다.
            if (_encounterFlow.RequestStart(player))
                gameObject.SetActive(false);
        }

        private void SetArmed(bool p_isArmed)
        {
            _trigger ??= GetComponent<Collider>();

            if (_trigger != null)
                _trigger.enabled = p_isArmed;
        }

        private void OnDisable()
        {
            if (_encounterFlow != null)
                _encounterFlow.OnIntroTriggerArmedChanged -= SetArmed;
        }

        private void OnValidate()
        {
            Collider trigger = GetComponent<Collider>();

            if (trigger != null)
                trigger.isTrigger = true;
        }
    }
}
