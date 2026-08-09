using Alpha.Combat;
using UnityEngine;

namespace Alpha.Item.Weapon.Range
{
    // 발사 즉시 Raycast하여 첫 번째 명중 대상에 피해를 전달한다.
    public class HitscanAttackModule : RangeAttackModule
    {
        [Header("Hit Detection")]
        [SerializeField]
        private LayerMask _hitMask;

        [SerializeField]
        private QueryTriggerInteraction _triggerInteraction =
            QueryTriggerInteraction.Ignore;

        protected override bool OnExecute(
            in RangeAttackRequest p_request)
        {
            Ray attackRay = new(
                p_request.Origin,
                p_request.Direction);

            bool hasHit = Physics.Raycast(
                attackRay,
                out RaycastHit hit,
                p_request.MaxDistance,
                _hitMask,
                _triggerInteraction);

            float drawDistance = hasHit
                ? hit.distance
                : p_request.MaxDistance;

            Debug.DrawRay(
                p_request.Origin,
                p_request.Direction * drawDistance,
                hasHit ? Color.yellow : Color.red,
                0.15f);

            // 빗나가거나 지형에 맞아도 발사 자체는 정상적으로 실행된 것이다.
            if (!hasHit)
                return true;

            DamageInfo damageInfo = new(
                p_request.Attacker,
                p_request.Damage,
                hit.point,
                hit.normal,
                p_request.Direction);

            DamageSystem.TryApply(
                hit.collider,
                damageInfo);

            return true;
        }
    }
}
