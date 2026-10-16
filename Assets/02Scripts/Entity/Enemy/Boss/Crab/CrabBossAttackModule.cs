using System.Collections.Generic;
using Alpha.Combat;
using Alpha.Detection;
using UnityEngine;

namespace Alpha.Enemy.Boss
{
    // 크랩 보스 공격의 실제 판정과 피해만 수행한다. 공격 선택과 타이밍은 Flow가 결정한다.
    [DisallowMultipleComponent]
    public sealed class CrabBossAttackModule : MonoBehaviour
    {
        private const int HitBufferCapacity = 128;

        [Header("Attack 1: Forward Claw Swing")]
        [Tooltip("Shape은 Forward Sector로 유지하고 Target Layers를 피격 대상에 맞게 설정합니다.")]
        [SerializeField]
        private DetectionAreaSettings _attack1Area = CreateDefaultAttack1Area();

        [SerializeField]
        private DamageProfile _attack1Damage = new();

        [Header("Attack 2: Targeted Area")]
        [Tooltip("Shape은 Radial로 유지합니다. 공격 위치는 Flow가 고정해 전달합니다.")]
        [SerializeField]
        private DetectionAreaSettings _attack2Area = CreateDefaultAttack2Area();

        [SerializeField]
        private DamageProfile _attack2Damage = new();

        private readonly Collider[] _overlapBuffer = new Collider[HitBufferCapacity];
        private readonly DetectionAreaHit[] _hitBuffer = new DetectionAreaHit[HitBufferCapacity];
        // 같은 범위 검사에서 대상의 여러 Collider가 중복 피해를 주지 않도록 한다.
        private readonly HashSet<IDamageable> _sampledTargets = new();

        private Transform _owner;
        private Transform _attack1Origin;
        private bool _hitWindowActive;

        // Core가 보스 루트와 Attack1 판정 기준점을 연결한다.
        public void Bind(Transform owner, Transform attackOrigin)
        {
            EndHitWindow();
            _owner = owner != null ? owner : transform;
            _attack1Origin = attackOrigin != null ? attackOrigin : _owner;
        }

        public void BeginHitWindow()
        {
            _sampledTargets.Clear();
            _hitWindowActive = true;
        }

        public void EndHitWindow()
        {
            _hitWindowActive = false;
            _sampledTargets.Clear();
        }

        // View도 실제 타격과 같은 원점, 방향, 범위 설정을 읽는다.
        public bool TryGetAttack1Area(out DetectionAreaRequest request)
        {
            if (!IsAttack1Configured())
            {
                request = default;
                return false;
            }

            request = new DetectionAreaRequest(
                AttackOrigin.position,
                AttackOrigin.forward,
                AttackOrigin.up,
                Owner,
                _attack1Area);

            return request.IsValid;
        }

        public bool TryGetAttack2Area(Vector3 targetPosition,
            out DetectionAreaRequest request)
        {
            if (!IsAttack2Configured())
            {
                request = default;
                return false;
            }

            request = new DetectionAreaRequest(
                targetPosition, Owner.forward, Vector3.up, Owner, _attack2Area);
            return request.IsValid;
        }

        // 열린 타격 구간에서 매 검사마다 판정한다. 재피격 여부는 대상이 결정한다.
        public void ApplyAttack1()
        {
            if (!_hitWindowActive || !IsAttack1Configured())
                return;

            int hitCount = CollectAttack1Hits();
            _sampledTargets.Clear();

            for (int index = 0; index < hitCount; index++)
            {
                DetectionAreaHit hit = _hitBuffer[index];
                if (!DamageSystem.TryGetDamageable(hit.Collider, out IDamageable damageable) ||
                    !_sampledTargets.Add(damageable))
                {
                    continue;
                }

                DamageInfo damageInfo = new(
                    Owner,
                    _attack1Damage.Damage,
                    hit.HitPoint,
                    -hit.Direction,
                    AttackOrigin.forward,
                    p_impact: _attack1Damage.Impact,
                    p_deliveryType: EDamageDeliveryType.Melee);

                DamageSystem.TryApply(hit.Collider, damageInfo);
            }
        }

        // Attack2는 IndicatorStart에서 고정한 위치를 구간 내내 사용한다.
        public void ApplyAttack2(Vector3 targetPosition)
        {
            if (!_hitWindowActive ||
                !TryGetAttack2Area(targetPosition, out DetectionAreaRequest request))
                return;

            Physics.SyncTransforms();
            int hitCount = DetectionAreaSystem.CollectHits(
                request, _overlapBuffer, _hitBuffer);
            _sampledTargets.Clear();

            for (int index = 0; index < hitCount; index++)
            {
                DetectionAreaHit hit = _hitBuffer[index];
                if (!DamageSystem.TryGetDamageable(hit.Collider, out IDamageable damageable) ||
                    !_sampledTargets.Add(damageable))
                {
                    continue;
                }

                Vector3 outward = Vector3.ProjectOnPlane(hit.Direction, Vector3.up);
                if (outward.sqrMagnitude <= 0.0001f)
                    outward = Owner.forward;

                DamageInfo damageInfo = new(
                    Owner, _attack2Damage.Damage, hit.HitPoint,
                    -hit.Direction, outward,
                    p_impact: _attack2Damage.Impact,
                    p_deliveryType: EDamageDeliveryType.Ranged);

                DamageSystem.TryApply(hit.Collider, damageInfo);
            }
        }

        private Transform Owner => _owner != null ? _owner : transform;
        private Transform AttackOrigin => _attack1Origin != null ? _attack1Origin : Owner;

        private bool IsAttack1Configured() =>
            _attack1Area != null &&
            _attack1Area.Shape == EDetectionAreaShape.ForwardSector &&
            _attack1Area.IsValid &&
            _attack1Damage != null &&
            _attack1Damage.IsValid;

        private bool IsAttack2Configured() =>
            _attack2Area != null &&
            _attack2Area.Shape == EDetectionAreaShape.Radial &&
            _attack2Area.IsValid &&
            _attack2Damage != null &&
            _attack2Damage.IsValid;

        private int CollectAttack1Hits()
        {
            if (!TryGetAttack1Area(out DetectionAreaRequest request))
                return 0;

            Physics.SyncTransforms();

            return DetectionAreaSystem.CollectHits(
                request,
                _overlapBuffer,
                _hitBuffer);
        }

        private void OnValidate()
        {
            _attack1Area ??= CreateDefaultAttack1Area();
            _attack1Damage ??= new DamageProfile();
            _attack2Area ??= CreateDefaultAttack2Area();
            _attack2Damage ??= new DamageProfile();
            _attack1Area.Validate();
            _attack1Damage.Validate();
            _attack2Area.Validate();
            _attack2Damage.Validate();
        }

        private static DetectionAreaSettings CreateDefaultAttack1Area() =>
            new(EDetectionAreaShape.ForwardSector, new Vector3(0f, 7.5f, 0f), 1 << 6,
                1f, 1f, 24f, 110f, 18f);

        private static DetectionAreaSettings CreateDefaultAttack2Area() =>
            new(EDetectionAreaShape.Radial, new Vector3(0f, 1.5f, 0f), 1 << 6,
                1f, 1f, 5f, 360f, 3f);
    }
}
