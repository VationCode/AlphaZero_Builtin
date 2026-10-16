using UnityEngine;

namespace Alpha.Enemy.Boss
{
    // GroundBody의 박스는 지면과 계속 충돌하고, 점프·돌진 중 플레이어 충돌만 잠시 푼다.
    [DisallowMultipleComponent]
    public sealed class CrabBossCollisionView : MonoBehaviour
    {
        [SerializeField] private BoxCollider[] _bodyColliders;
        [SerializeField, Min(0f)] private float _contactClearancePadding = 0.15f;

        private CharacterController _ignoredPlayer;
        private bool _restoreRequested;

        public bool IsContactRestorePending => _restoreRequested;
        public bool HasBodyColliders => _bodyColliders != null && _bodyColliders.Length > 0;

        public void SetContactBlocking(bool blocking, Transform target = null)
        {
            if (blocking)
            {
                _restoreRequested = _ignoredPlayer != null;
                TryRestoreContactBlocking();
                return;
            }

            CharacterController player = target != null
                ? target.GetComponentInParent<CharacterController>() ??
                  target.GetComponentInChildren<CharacterController>()
                : null;
            if (player == null || !HasBodyColliders)
                return;

            if (_ignoredPlayer != null && _ignoredPlayer != player)
                RestoreContactBlockingImmediately();

            _ignoredPlayer = player;
            _restoreRequested = false;
            SetPlayerCollisionIgnored(true);
        }

        // 겹친 박스 중 플레이어를 가장 멀리 밀어내야 하는 방향을 반환한다.
        public bool TryGetPlayerClearance(out CharacterController player, out Vector3 displacement)
        {
            player = _ignoredPlayer;
            displacement = Vector3.zero;
            if (player == null || !HasBodyColliders)
                return false;

            Physics.SyncTransforms();
            float greatestDistance = 0f;
            Vector3 playerCenter = player.bounds.center;
            Vector3 playerScale = player.transform.lossyScale;
            float playerRadius = player.radius * Mathf.Max(
                Mathf.Abs(playerScale.x), Mathf.Abs(playerScale.z));
            for (int index = 0; index < _bodyColliders.Length; index++)
            {
                BoxCollider body = _bodyColliders[index];
                if (body == null || !body.enabled ||
                    !Physics.ComputePenetration(body, body.transform.position,
                        body.transform.rotation, player, player.transform.position,
                        player.transform.rotation, out _, out _))
                    continue;

                Vector3 center = body.transform.TransformPoint(body.center);
                Vector3 toPlayer = playerCenter - center;
                Vector3 right = Vector3.ProjectOnPlane(body.transform.right, Vector3.up).normalized;
                Vector3 forward = Vector3.ProjectOnPlane(body.transform.forward, Vector3.up).normalized;
                Vector3 scale = body.transform.lossyScale;
                float x = Vector3.Dot(toPlayer, right);
                float z = Vector3.Dot(toPlayer, forward);
                float exitX = body.size.x * Mathf.Abs(scale.x) * 0.5f +
                              playerRadius - Mathf.Abs(x);
                float exitZ = body.size.z * Mathf.Abs(scale.z) * 0.5f +
                              playerRadius - Mathf.Abs(z);
                bool exitAlongX = exitX <= exitZ;
                float distance = exitAlongX ? exitX : exitZ;
                if (distance <= greatestDistance)
                    continue;

                greatestDistance = distance;
                Vector3 direction = exitAlongX
                    ? right * (x >= 0f ? 1f : -1f)
                    : forward * (z >= 0f ? 1f : -1f);
                displacement = direction * (distance + _contactClearancePadding);
            }

            return greatestDistance > 0f;
        }

        public bool TryRestoreContactBlocking()
        {
            if (!_restoreRequested)
                return false;

            if (_ignoredPlayer != null && TryGetPlayerClearance(out _, out _))
                return false;

            RestoreContactBlockingImmediately();
            return true;
        }

        private void SetPlayerCollisionIgnored(bool ignored)
        {
            if (_ignoredPlayer == null || !HasBodyColliders)
                return;

            for (int index = 0; index < _bodyColliders.Length; index++)
            {
                BoxCollider body = _bodyColliders[index];
                if (body != null)
                    Physics.IgnoreCollision(body, _ignoredPlayer, ignored);
            }
        }

        private void RestoreContactBlockingImmediately()
        {
            SetPlayerCollisionIgnored(false);
            _ignoredPlayer = null;
            _restoreRequested = false;
        }

        private void FixedUpdate()
        {
            if (_restoreRequested)
                TryRestoreContactBlocking();
        }

        private void OnDisable() => RestoreContactBlockingImmediately();

        private void OnValidate() =>
            _contactClearancePadding = Mathf.Max(0f, _contactClearancePadding);
    }
}
