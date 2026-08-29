using Alpha.Detection;
using Alpha.Living;
using UnityEngine;

namespace Alpha.Enemy
{
    // 감지 결과를 현재 타깃으로 유지·교체하고 Combat과 Locomotion에 공유한다.
    public sealed class EnemyTargetingFlow
    {
        private EnemyCore _core;
        private float _nextSearchTime;

        public void Bind(EnemyCore p_core)
        {
            _core = p_core;
            _nextSearchTime = Time.time;
        }

        // 복귀 중이 아닐 때 기존 타깃을 검증하고 감지 주기에 맞춰 새 타깃을 찾는다.
        public void Tick()
        {
            if (_core == null ||
                _core.LocomotionFlow.IsReturningToArea)
            {
                return;
            }

            AreaDetectionModule detection =
                _core.TargetDetectionModule;
            Transform currentTarget = _core.Target;

            if (IsValidTarget(currentTarget))
            {
                return;
            }

            if (currentTarget != null)
                ClearTarget();

            if (detection == null ||
                !detection.isActiveAndEnabled ||
                Time.time < _nextSearchTime)
            {
                return;
            }

            ScheduleNextSearch();

            if (TryFindClosestTarget(
                    detection,
                    out Transform foundTarget))
            {
                _core.SetTarget(foundTarget);
            }
        }

        // 추적 경계 안의 유효한 공격자를 일반 감지 대상보다 우선한다.
        public bool TryPrioritizeTarget(Transform p_target)
        {
            if (_core == null)
                return false;

            EnemyLocomotionModule locomotion = _core.LocomotionModule;

            if (locomotion == null ||
                !IsValidTarget(p_target) ||
                locomotion.IsOutsideChaseArea(p_target.position))
            {
                return false;
            }

            if (_core.Target != p_target)
            {
                _core.CombatFlow?.CancelCombat();
                _core.SetTarget(p_target);
            }

            ScheduleNextSearch();
            return true;
        }

        public void ClearTarget()
        {
            if (_core == null)
                return;

            _core.CombatFlow?.CancelCombat();
            _core.ClearTarget();
        }

        public void Reset()
        {
            ClearTarget();
            _nextSearchTime = Time.time;
        }

        // 공용 감지 결과를 Enemy 규칙으로 해석해 가장 가까운 Living을 고른다.
        private bool TryFindClosestTarget(
            AreaDetectionModule p_detection,
            out Transform p_target)
        {
            p_target = null;
            int hitCount = p_detection.CollectHits();
            Vector3 areaOrigin = p_detection.AreaOrigin;
            float closestDistanceSqr = float.PositiveInfinity;

            for (int index = 0; index < hitCount; index++)
            {
                if (!p_detection.TryGetHit(
                        index,
                        out DetectionAreaHit hit))
                {
                    continue;
                }

                Transform candidate =
                    ResolveTargetRoot(hit.Collider.transform);

                if (!IsValidTarget(candidate))
                    continue;

                float distanceSqr =
                    (hit.HitPoint - areaOrigin).sqrMagnitude;

                if (distanceSqr >= closestDistanceSqr)
                    continue;

                closestDistanceSqr = distanceSqr;
                p_target = candidate;
            }

            return p_target != null;
        }

        private bool IsValidTarget(Transform p_target)
        {
            AreaDetectionModule detection =
                _core?.TargetDetectionModule;
            DetectionAreaSettings settings = detection?.Settings;

            if (detection == null ||
                !detection.isActiveAndEnabled ||
                p_target == null ||
                !p_target.gameObject.activeInHierarchy ||
                settings == null ||
                (settings.TargetLayers.value &
                 (1 << p_target.gameObject.layer)) == 0)
            {
                return false;
            }

            LivingModule livingModule =
                p_target.GetComponentInChildren<LivingModule>(true);

            return livingModule != null &&
                   livingModule.IsBound &&
                   !livingModule.IsDead;
        }

        private static Transform ResolveTargetRoot(Transform p_hit)
        {
            Transform target = p_hit;
            int targetLayer = p_hit.gameObject.layer;

            while (target.parent != null &&
                   target.parent.gameObject.layer == targetLayer)
            {
                target = target.parent;
            }

            return target;
        }

        private void ScheduleNextSearch()
        {
            _nextSearchTime =
                Time.time + _core.TargetSearchInterval;
        }
    }
}
