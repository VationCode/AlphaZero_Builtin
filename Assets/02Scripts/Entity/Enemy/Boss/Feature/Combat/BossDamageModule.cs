using Alpha.Combat;
using Alpha.Detection;
using UnityEngine;

namespace Alpha.Boss
{
    // Melee·Rush 영역을 검색하여 같은 공격에서 대상별 한 번만 공용 피해 계약으로 전달한다.
    public sealed class BossDamageModule
    {
        private readonly Collider[] _overlapBuffer = new Collider[64];
        private readonly DetectionAreaHit[] _hitBuffer = new DetectionAreaHit[64];

        public void Apply(Transform p_owner, Vector3 p_position, Vector3 p_forward,
            BossCombatContext p_context, bool p_sweep)
        {
            BossPatternData pattern = p_context?.CurrentPattern;
            if (p_owner == null || pattern?.Damage == null || !pattern.Damage.Enabled)
                return;
            BossDamageContext damageContext = p_context.Damage;
            long attackId = p_context.AttackId;

            DetectionAreaSettings area = pattern.Damage.Area;
            Vector3 previous = p_sweep && damageContext.HasPosition ? damageContext.PreviousPosition : p_position;
            damageContext.PreviousPosition = p_position;
            damageContext.HasPosition = true;
            // 빠른 돌진도 두 물리 위치 사이를 함께 검사한다. 완료 판정은 현재 위치만 검사한다.
            float minimumSize = area.Shape == EDetectionAreaShape.ForwardBox
                ? Mathf.Min(area.Width, area.Length, area.Height) : Mathf.Min(area.Radius, area.Height);
            int samples = Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(previous, p_position) /
                Mathf.Max(0.1f, minimumSize * 0.5f)), 1, 64);
            Physics.SyncTransforms();
            for (int sample = 1; sample <= samples; sample++)
            {
                if (!IsCurrent(p_context, attackId, p_owner))
                    return;
                DetectionAreaRequest request = new(Vector3.Lerp(previous, p_position, (float)sample / samples),
                    p_forward, Vector3.up, p_owner, area);
                int count = DetectionAreaSystem.CollectHits(request, _overlapBuffer, _hitBuffer);
                for (int i = 0; i < count; i++)
                {
                    if (!IsCurrent(p_context, attackId, p_owner))
                        return;
                    DetectionAreaHit hit = _hitBuffer[i];
                    if (!DamageSystem.TryGetDamageable(hit.Collider, out IDamageable target) ||
                        !damageContext.DamagedTargets.Add(target))
                        continue;
                    DamageProfile profile = pattern.DamageProfile;
                    DamageInfo damage = new(p_owner, profile.Damage, hit.HitPoint, -hit.Direction.normalized,
                        hit.Direction, profile.Impact, EDamageDeliveryType.Melee);
                    if (!DamageSystem.TryApply(hit.Collider, damage) && IsCurrent(p_context, attackId, p_owner))
                        damageContext.DamagedTargets.Remove(target);
                }
            }
        }

        private static bool IsCurrent(BossCombatContext p_context, long p_id, Transform p_owner) =>
            p_owner != null && p_context.State == EBossCombatState.Attack && p_context.AttackId == p_id;
    }
}
