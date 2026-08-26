using Alpha.Detection;
using Alpha.Living;
using Alpha.Utility;
using UnityEngine;

namespace Alpha.Enemy
{
    // 공용 DetectionAreaSystem 결과에서 가장 가까운 Living 대상을 찾는다.
    [DisallowMultipleComponent]
    public sealed class EnemyDetectionModule : MonoBehaviour
    {
        private const int DetectionBufferCapacity = 16;

        [SerializeField, Min(0.05f)]
        private float _scanInterval = 0.2f;

        [SerializeField]
        private Transform _detectionOrigin;

        [SerializeField]
        private DetectionAreaSettings _detectionArea = new();

        private readonly Collider[] _detectionBuffer =
            new Collider[DetectionBufferCapacity];

        private Transform _owner;

        public float ScanInterval => Mathf.Max(0.05f, _scanInterval);
        public DetectionAreaSettings DetectionArea => _detectionArea;

        public void Bind(Transform p_owner)
        {
            _owner = p_owner;
            _detectionOrigin ??= p_owner;
        }

        // 감지 범위 안에서 살아 있는 가장 가까운 대상을 반환한다.
        public bool TryDetectClosestTarget(out Transform p_target)
        {
            p_target = null;

            Transform origin = ResolveDetectionOrigin();

            if (!isActiveAndEnabled ||
                _owner == null ||
                origin == null ||
                _detectionArea == null ||
                !_detectionArea.IsValid)
            {
                return false;
            }

            DetectionAreaRequest request = new(
                origin.position,
                origin.forward,
                origin.up,
                _owner,
                _detectionArea);

            int resultCount = DetectionAreaSystem.Query(
                request,
                _detectionBuffer);

            float closestDistanceSqr = float.PositiveInfinity;

            for (int index = 0; index < resultCount; index++)
            {
                Collider hit = _detectionBuffer[index];

                if (hit == null)
                    continue;

                Transform candidate = ResolveTargetRoot(hit.transform);

                if (!IsValidTarget(candidate))
                    continue;

                Vector3 hitPoint =
                    ColliderPointUtility.GetClosestPoint(
                        hit,
                        request.AreaOrigin);
                float distanceSqr =
                    (hitPoint - request.AreaOrigin).sqrMagnitude;

                if (distanceSqr >= closestDistanceSqr)
                    continue;

                closestDistanceSqr = distanceSqr;
                p_target = candidate;
            }

            return p_target != null;
        }

        public bool IsValidTarget(Transform p_target)
        {
            if (!isActiveAndEnabled ||
                p_target == null ||
                !p_target.gameObject.activeInHierarchy ||
                _detectionArea == null ||
                (_detectionArea.TargetMask.value &
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

        private Transform ResolveDetectionOrigin()
        {
            if (_detectionOrigin != null)
                return _detectionOrigin;

            return _owner != null
                ? _owner
                : GetComponentInParent<EnemyCore>()?.transform;
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

        private void OnValidate()
        {
            _scanInterval = Mathf.Max(0.05f, _scanInterval);
            _detectionArea ??= new DetectionAreaSettings();
            _detectionArea.Validate();
        }

        private void OnDrawGizmosSelected()
        {
            DetectionAreaGizmoDrawer.Draw(
                ResolveDetectionOrigin(),
                _detectionArea,
                new Color(1f, 0.35f, 0.1f));
        }
    }
}
