using System.Collections.Generic;
using Alpha.Player;
using UnityEngine;

namespace Alpha.Enemy.View
{
    // 큰 Boss 구역에서 Player의 마지막 Collider가 나갈 때 Encounter 재설정을 요청한다.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class BossEncounterResetBoundaryView : MonoBehaviour
    {
        [SerializeField]
        private CrabBossEncounterFlow _encounterFlow;

        private readonly Dictionary<PlayerCore, int> _overlapCounts = new();

        private void OnTriggerEnter(Collider p_other)
        {
            PlayerCore player = p_other.GetComponentInParent<PlayerCore>();

            if (player == null)
                return;

            _overlapCounts.TryGetValue(player, out int count);
            _overlapCounts[player] = count + 1;
        }

        private void OnTriggerExit(Collider p_other)
        {
            PlayerCore player = p_other.GetComponentInParent<PlayerCore>();

            if (player == null ||
                !_overlapCounts.TryGetValue(player, out int count))
            {
                return;
            }

            if (count > 1)
            {
                _overlapCounts[player] = count - 1;
                return;
            }

            _overlapCounts.Remove(player);
            _encounterFlow?.RequestReset(player);
        }

        private void OnDisable()
        {
            _overlapCounts.Clear();
        }

        private void OnValidate()
        {
            Collider trigger = GetComponent<Collider>();

            if (trigger != null)
                trigger.isTrigger = true;
        }
    }
}
