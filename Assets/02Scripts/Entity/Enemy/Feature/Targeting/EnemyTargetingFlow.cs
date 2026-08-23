using UnityEngine;

namespace Alpha.Enemy
{
    // 감지 결과를 현재 타깃으로 유지·교체하고 Combat과 Locomotion에 공유한다.
    public sealed class EnemyTargetingFlow
    {
        private EnemyCore _core;
        private float _nextScanTime;

        public void Bind(EnemyCore p_core)
        {
            _core = p_core;
            _nextScanTime = Time.time;
        }

        // 복귀 중이 아닐 때 기존 타깃을 검증하고 감지 주기에 맞춰 새 타깃을 찾는다.
        public void Tick()
        {
            if (_core == null ||
                _core.LocomotionFlow.IsReturningToPatrol)
            {
                return;
            }

            EnemyDetectionModule detection = _core.TargetModule;
            Transform currentTarget = _core.Target;

            if (detection != null &&
                detection.IsValidTarget(currentTarget))
            {
                return;
            }

            if (currentTarget != null)
                ClearTarget();

            if (detection == null || Time.time < _nextScanTime)
                return;

            ScheduleNextScan(detection);

            if (detection.TryDetectClosestTarget(
                    out Transform detectedTarget))
            {
                _core.SetTarget(detectedTarget);
            }
        }

        // 추적 경계 안의 유효한 공격자를 일반 감지 대상보다 우선한다.
        public bool TryPrioritizeTarget(Transform p_target)
        {
            if (_core == null)
                return false;

            EnemyDetectionModule detection = _core.TargetModule;
            EnemyLocomotionModule locomotion = _core.LocomotionModule;

            if (detection == null ||
                locomotion == null ||
                !detection.IsValidTarget(p_target) ||
                locomotion.IsOutsideChaseBoundary(p_target.position))
            {
                return false;
            }

            if (_core.Target != p_target)
            {
                _core.CombatFlow?.CancelCombat();
                _core.SetTarget(p_target);
            }

            ScheduleNextScan(detection);
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
            _nextScanTime = Time.time;
        }

        private void ScheduleNextScan(EnemyDetectionModule p_detection)
        {
            _nextScanTime =
                Time.time + p_detection.ScanInterval;
        }
    }
}
