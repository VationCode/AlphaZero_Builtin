using UnityEngine;

namespace Alpha.Enemy.Boss
{
    // Attack3 동안 물리 접촉을 풀되 보스의 피격 대상 판정은 유지한다.
    [DisallowMultipleComponent]
    public sealed class CrabBossCollisionView : MonoBehaviour
    {
        private const int OverlapCapacity = 128;

        [SerializeField] private SphereCollider _contactCollider;
        [SerializeField] private SphereCollider _attackTargetCollider;
        [SerializeField, Min(0f)] private float _contactClearancePadding = 0.15f;

        private readonly Collider[] _overlaps = new Collider[OverlapCapacity];
        private bool _restoreRequested;

        public bool IsContactRestorePending => _restoreRequested;

        public void SetContactBlocking(bool blocking)
        {
            if (_contactCollider == null || _attackTargetCollider == null)
                return;

            if (blocking)
            {
                _restoreRequested = true;
                TryRestoreContactBlocking();
                return;
            }

            _restoreRequested = false;
            _attackTargetCollider.center = _contactCollider.center;
            _attackTargetCollider.radius = _contactCollider.radius;
            _attackTargetCollider.enabled = true;
            _contactCollider.enabled = false;
        }

        // 실제 피해 여부와 관계없이 몸 안에 남은 Player의 수평 퇴출 거리를 제공한다.
        public bool TryGetPlayerClearance(out CharacterController player, out Vector3 displacement)
        {
            player = null;
            displacement = Vector3.zero;
            Physics.SyncTransforms();
            if (!TryFindOverlap(out Collider overlap, playerOnly: true) ||
                overlap is not CharacterController controller)
                return false;

            Vector3 center = ContactCenter;
            Vector3 offset = controller.bounds.center - center;
            offset.y = 0f;
            Vector3 direction = offset.sqrMagnitude > 0.0001f
                ? offset.normalized
                : Vector3.ProjectOnPlane(_contactCollider.transform.forward, Vector3.up).normalized;
            if (direction.sqrMagnitude <= 0.0001f)
                direction = Vector3.forward;

            Vector3 scale = controller.transform.lossyScale;
            float playerRadius = controller.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            float exitDistance = ContactRadius + playerRadius + _contactClearancePadding;
            displacement = direction * Mathf.Max(0f, exitDistance - offset.magnitude);
            player = controller;
            return displacement.sqrMagnitude > 0.0001f;
        }

        public bool TryRestoreContactBlocking()
        {
            if (!_restoreRequested || _contactCollider == null || _attackTargetCollider == null)
                return false;

            // CharacterController.Move 직후에도 최신 위치로 검사한다.
            Physics.SyncTransforms();
            if (TryFindOverlap(out _, playerOnly: false))
                return false;

            _contactCollider.enabled = true;
            _attackTargetCollider.enabled = false;
            _restoreRequested = false;
            return true;
        }

        private bool TryFindOverlap(out Collider overlap, bool playerOnly)
        {
            overlap = null;
            if (_contactCollider == null)
                return false;

            int mask = ~_contactCollider.excludeLayers.value;
            int count = Physics.OverlapSphereNonAlloc(ContactCenter, ContactRadius,
                _overlaps, mask, QueryTriggerInteraction.Ignore);
            // 버퍼가 가득 차면 안전하게 접촉 복구를 보류한다.
            if (count == _overlaps.Length && !playerOnly)
                return true;

            Rigidbody owner = _contactCollider.attachedRigidbody;
            for (int index = 0; index < count; index++)
            {
                Collider candidate = _overlaps[index];
                if (candidate == null || candidate == _contactCollider ||
                    candidate == _attackTargetCollider || !candidate.enabled ||
                    (owner != null && candidate.attachedRigidbody == owner) ||
                    Physics.GetIgnoreLayerCollision(_contactCollider.gameObject.layer,
                        candidate.gameObject.layer))
                    continue;

                if (playerOnly &&
                    (candidate is not CharacterController || !candidate.CompareTag("Player")))
                    continue;

                Collider probe = _contactCollider.enabled
                    ? _contactCollider : _attackTargetCollider;
                if (!Physics.ComputePenetration(probe,
                        probe.transform.position, probe.transform.rotation,
                        candidate, candidate.transform.position, candidate.transform.rotation,
                        out _, out _))
                    continue;

                overlap = candidate;
                return true;
            }

            return false;
        }

        private Vector3 ContactCenter =>
            _contactCollider.transform.TransformPoint(_contactCollider.center);

        private float ContactRadius
        {
            get
            {
                Vector3 scale = _contactCollider.transform.lossyScale;
                return _contactCollider.radius * Mathf.Max(
                    Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            }
        }

        private void FixedUpdate()
        {
            if (_restoreRequested)
                TryRestoreContactBlocking();
        }

        private void OnDisable() => SetContactBlocking(true);

        private void OnValidate() =>
            _contactClearancePadding = Mathf.Max(0f, _contactClearancePadding);
    }
}
