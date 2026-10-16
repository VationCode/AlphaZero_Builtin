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

        [Header("Attack 3: Jump Landing")]
        [Tooltip("착지 범위와 예고 표시의 기준이 되는 보스의 지면 BoxCollider입니다.")]
        [SerializeField] private BoxCollider _attack3LandingCollider;

        [Tooltip("착지 공격의 피격 대상 레이어입니다. 범위 모양은 BoxCollider를 따릅니다.")]
        [SerializeField] private LayerMask _attack3TargetLayers = 1 << 6;

        [SerializeField]
        private DamageProfile _attack3Damage = new();

        private readonly Collider[] _overlapBuffer = new Collider[HitBufferCapacity];
        private readonly DetectionAreaHit[] _hitBuffer = new DetectionAreaHit[HitBufferCapacity];
        // 같은 범위 검사에서 대상의 여러 Collider가 중복 피해를 주지 않도록 한다.
        private readonly HashSet<IDamageable> _sampledTargets = new();

        private Transform _owner;
        private Transform _attack1Origin;
        private DetectionAreaSettings _attack3Area;
        private Vector3 _cachedAttack3Center;
        private Vector3 _cachedAttack3Size;
        private int _cachedAttack3TargetLayers;
        private bool _hitWindowActive;

        // Attack3의 실제 충격 설정과 같은 거리/시간으로 접촉 해소 속도를 계산한다.
        public bool TryGetAttack3KnockbackSpeed(out float speed)
        {
            speed = 0f;
            if (_attack3Damage == null)
                return false;

            AttackImpactInfo impact = _attack3Damage.Impact;
            if (impact.KnockbackDistance <= 0f || impact.KnockbackDuration <= 0f)
                return false;

            speed = impact.KnockbackDistance / impact.KnockbackDuration;
            return true;
        }

        // Core가 보스 루트와 Attack1 판정 기준점을 연결한다.
        public void Bind(Transform owner, Transform attackOrigin)
        {
            EndHitWindow();
            _owner = owner != null ? owner : transform;
            _attack1Origin = attackOrigin != null ? attackOrigin : _owner;
            // 씬 참조가 비어도 보스의 지면 BoxCollider를 다시 찾는다.
            if (_attack3LandingCollider == null)
            {
                Transform groundBody = _owner.Find("GroundBody");
                if (groundBody != null)
                    _attack3LandingCollider = groundBody.GetComponent<BoxCollider>();
            }
            _attack3Area = null;
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

        public bool TryGetAttack3Area(Vector3 landingPosition,
            out DetectionAreaRequest request)
        {
            if (!IsAttack3Configured())
            {
                request = default;
                return false;
            }

            // 착지 판정과 장판 표시가 실제 지면 BoxCollider의 크기를 공유한다.
            RefreshAttack3Area();
            request = new DetectionAreaRequest(
                landingPosition, Owner.forward, Vector3.up, Owner, _attack3Area);
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

            ApplyAreaAttack(request, _attack2Damage, EDamageDeliveryType.Ranged);
        }

        public void ApplyAttack3(Vector3 landingPosition)
        {
            if (!_hitWindowActive ||
                !TryGetAttack3Area(landingPosition, out DetectionAreaRequest request))
                return;

            ApplyAreaAttack(request, _attack3Damage, EDamageDeliveryType.Melee);
        }

        private void ApplyAreaAttack(in DetectionAreaRequest request,
            DamageProfile damage, EDamageDeliveryType deliveryType)
        {
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
                    Owner, damage.Damage, hit.HitPoint,
                    -hit.Direction, outward,
                    p_impact: damage.Impact,
                    p_deliveryType: deliveryType);

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

        private bool IsAttack3Configured() =>
            _attack3LandingCollider != null &&
            _attack3LandingCollider.enabled &&
            _attack3LandingCollider.size.x > 0f &&
            _attack3LandingCollider.size.y > 0f &&
            _attack3LandingCollider.size.z > 0f &&
            _attack3TargetLayers.value != 0 &&
            _attack3Damage != null &&
            _attack3Damage.IsValid;

        private void RefreshAttack3Area()
        {
            Vector3 worldCenter = _attack3LandingCollider.transform.TransformPoint(
                _attack3LandingCollider.center);
            Vector3 center = Quaternion.Inverse(Owner.rotation) *
                             (worldCenter - Owner.position);
            Vector3 scale = _attack3LandingCollider.transform.lossyScale;
            Vector3 colliderSize = _attack3LandingCollider.size;
            Vector3 size = new(
                colliderSize.x * Mathf.Abs(scale.x),
                colliderSize.y * Mathf.Abs(scale.y),
                colliderSize.z * Mathf.Abs(scale.z));

            if (_attack3Area != null && _cachedAttack3Center == center &&
                _cachedAttack3Size == size &&
                _cachedAttack3TargetLayers == _attack3TargetLayers.value)
                return;

            // ForwardBox는 앞쪽으로 뻗으므로 원점을 Box의 뒤쪽 면에 둔다.
            Vector3 origin = center - Vector3.forward * (size.z * 0.5f);
            _attack3Area = new DetectionAreaSettings(
                EDetectionAreaShape.ForwardBox, origin, _attack3TargetLayers,
                size.x, size.z, 1f, 360f, size.y);
            _cachedAttack3Center = center;
            _cachedAttack3Size = size;
            _cachedAttack3TargetLayers = _attack3TargetLayers.value;
        }

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
            _attack3Damage ??= new DamageProfile();
            _attack1Area.Validate();
            _attack1Damage.Validate();
            _attack2Area.Validate();
            _attack2Damage.Validate();
            _attack3Damage.Validate();
            _attack3Area = null;
        }

        private static DetectionAreaSettings CreateDefaultAttack1Area() =>
            new(EDetectionAreaShape.ForwardSector, new Vector3(0f, 7.5f, 0f), 1 << 6,
                1f, 1f, 24f, 110f, 18f);

        private static DetectionAreaSettings CreateDefaultAttack2Area() =>
            new(EDetectionAreaShape.Radial, new Vector3(0f, 1.5f, 0f), 1 << 6,
                1f, 1f, 5f, 360f, 3f);
    }
}
