using Alpha.Player;
using UnityEngine;

namespace Alpha.Boss
{
    // 구형 감지 영역에서 살아 있는 Player를 찾아 대표 Transform으로 반환한다.
    public sealed class BossTargetDetectionModule
    {
        private readonly Collider[] _hits = new Collider[64];

        public bool TryFindClosest(Vector3 p_position, float p_radius, LayerMask p_layers, out Transform p_target)
        {
            p_target = null;
            int count = Physics.OverlapSphereNonAlloc(p_position, p_radius, _hits, p_layers, QueryTriggerInteraction.Collide);
            // 콜라이더가 많은 Scene에서도 가까운 Player를 누락하지 않는다.
            Collider[] hits = count == _hits.Length
                ? Physics.OverlapSphere(p_position, p_radius, p_layers, QueryTriggerInteraction.Collide) : _hits;
            if (hits != _hits)
                count = hits.Length;
            float closest = p_radius * p_radius;
            for (int i = 0; i < count; i++)
            {
                if (hits[i] == null)
                    continue;
                Transform candidate = ResolvePlayer(hits[i].transform);
                if (candidate == null)
                    continue;
                float distance = (candidate.position - p_position).sqrMagnitude;
                if (distance > closest)
                    continue;
                closest = distance;
                p_target = candidate;
            }
            return p_target != null;
        }

        // Collider 자식 또는 외부에서 받은 Transform도 Player의 대표 객체로 정규화한다.
        public Transform ResolvePlayer(Transform p_target)
        {
            PlayerCore player = p_target != null ? p_target.GetComponentInParent<PlayerCore>() : null;
            return player != null && player.isActiveAndEnabled && player.gameObject.activeInHierarchy &&
                player.HealthContext.MaxHealth > 0f && !player.HealthContext.IsDead ? player.transform : null;
        }
    }
}
