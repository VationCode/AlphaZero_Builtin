using System.Collections.Generic;
using Alpha.Combat;
using Alpha.Detection;
using UnityEngine;

namespace Alpha.Enemy
{
    // Rush 패턴의 목적지 이동과 돌진 중 대상별 1회 피해를 수행한다.
    public sealed class EnemyRushAttackModule
    {
        private const int HitBufferCapacity = 32;

        private readonly Collider[] _overlapBuffer =
            new Collider[HitBufferCapacity];

        private readonly DetectionAreaHit[] _hitBuffer =
            new DetectionAreaHit[HitBufferCapacity];

        private readonly HashSet<IDamageable> _damagedTargets = new();

        private Vector3 _startPosition;
        private Vector3 _destination;
        private float _animationDuration;
        private float _animationElapsedTime;
        private float _movementStartTime;
        private float _movementEndTime;
        private float _appliedMovementProgress;
        private bool _hasAnimationTime;

        public bool IsActive { get; private set; }

        public void Begin(
            Transform p_owner,
            Transform p_target,
            EnemyAttackPatternSetting p_pattern)
        {
            if (p_owner == null || p_pattern == null)
            {
                End();
                return;
            }

            _startPosition = p_owner.position;
            _destination = p_target != null
                ? p_target.position
                : _startPosition;
            _destination.y = _startPosition.y;

            _animationDuration = 0f;
            _animationElapsedTime = 0f;
            _movementStartTime = 0f;
            _movementEndTime = 0f;
            _appliedMovementProgress = 0f;
            _hasAnimationTime = false;

            _damagedTargets.Clear();
            IsActive = true;
        }

        public void Tick(
            Transform p_owner,
            EnemyLocomotionModule p_locomotion,
            EnemyAttackPatternSetting p_pattern,
            float p_deltaTime)
        {
            if (!IsActive ||
                p_owner == null ||
                p_locomotion == null ||
                p_pattern == null ||
                !_hasAnimationTime)
            {
                return;
            }

            float deltaTime = Mathf.Max(0f, p_deltaTime);
            _animationElapsedTime = Mathf.Min(
                _animationDuration,
                _animationElapsedTime + deltaTime);

            float movementDuration =
                _movementEndTime - _movementStartTime;
            float normalizedTime = movementDuration > 0f
                ? Mathf.Clamp01(
                    (_animationElapsedTime - _movementStartTime) /
                    movementDuration)
                : _animationElapsedTime >= _movementEndTime
                    ? 1f
                    : 0f;
            float movementProgress = Mathf.Max(
                _appliedMovementProgress,
                p_pattern.EvaluateRushMovement(normalizedTime));

            Vector3 totalDisplacement =
                _destination - _startPosition;
            Vector3 frameDisplacement = totalDisplacement *
                (movementProgress - _appliedMovementProgress);

            p_locomotion.MoveByAnimation(
                frameDisplacement,
                _destination,
                deltaTime);

            _appliedMovementProgress = movementProgress;

            if (p_pattern.RushArea.IsActive(
                    _animationElapsedTime))
            {
                ApplyDamage(p_owner, p_pattern);
            }
        }

        // 실제 Animation 시간 중 점프 시작부터 착지 시점까지만 이동 구간으로 사용한다.
        public void SynchronizeAnimationTime(
            float p_elapsedSeconds,
            float p_durationSeconds,
            float p_jumpStartTimeSeconds,
            float p_landingTimeSeconds)
        {
            if (!IsActive || p_durationSeconds <= 0f)
                return;

            _animationDuration = p_durationSeconds;
            _movementStartTime = Mathf.Clamp(
                p_jumpStartTimeSeconds,
                0f,
                _animationDuration);
            _movementEndTime = Mathf.Clamp(
                p_landingTimeSeconds,
                _movementStartTime,
                _animationDuration);
            _animationElapsedTime = Mathf.Max(
                _animationElapsedTime,
                Mathf.Clamp(
                    p_elapsedSeconds,
                    0f,
                    _animationDuration));
            _hasAnimationTime = true;
        }

        // 마지막 물리 Tick이 남은 이동량을 적용할 수 있도록 진행률을 끝으로 맞춘다.
        public void CompleteAnimationTime()
        {
            if (!IsActive || !_hasAnimationTime)
                return;

            _animationElapsedTime = _animationDuration;
        }

        public void End()
        {
            IsActive = false;
            _animationDuration = 0f;
            _animationElapsedTime = 0f;
            _movementStartTime = 0f;
            _movementEndTime = 0f;
            _appliedMovementProgress = 0f;
            _hasAnimationTime = false;
            _damagedTargets.Clear();
        }

        private void ApplyDamage(
            Transform p_owner,
            EnemyAttackPatternSetting p_pattern)
        {
            Physics.SyncTransforms();

            DetectionAreaRequest request = new(
                p_owner.position,
                p_owner.forward,
                p_owner.up,
                p_owner,
                p_pattern.RushArea);

            int hitCount = DetectionAreaSystem.CollectHits(
                request,
                _overlapBuffer,
                _hitBuffer);

            for (int index = 0; index < hitCount; index++)
            {
                DetectionAreaHit hit = _hitBuffer[index];
                if (!DamageSystem.TryGetDamageable(
                        hit.Collider,
                        out IDamageable damageable) ||
                    !_damagedTargets.Add(damageable))
                {
                    continue;
                }

                DamageProfile profile = p_pattern.DamageProfile;
                DamageInfo damageInfo = new(
                    p_owner,
                    profile.Damage,
                    hit.HitPoint,
                    -hit.Direction,
                    p_owner.forward,
                    p_impact: profile.Impact,
                    p_deliveryType: EDamageDeliveryType.Melee);

                if (!DamageSystem.TryApply(
                        hit.Collider,
                        damageInfo))
                {
                    continue;
                }

            }
        }
    }
}
