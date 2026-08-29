using UnityEngine;

namespace Alpha.AI
{
    // 순찰 영역과 현재 순찰 지점을 Scene Gizmo로 표현한다.
    [DisallowMultipleComponent]
    public sealed class PatrolGizmoView : MonoBehaviour
    {
        private const int CircleSegments = 48;
        private const float PointMarkerRadius = 0.15f;

        [SerializeField]
        private PatrolModule _patrolModule;

        private void Reset()
        {
            _patrolModule = GetComponent<PatrolModule>();
        }

        private void OnValidate()
        {
            _patrolModule ??= GetComponent<PatrolModule>();
        }

        private void OnDrawGizmosSelected()
        {
            _patrolModule ??= GetComponent<PatrolModule>();

            if (_patrolModule == null)
                return;

            Vector3 center =
                Application.isPlaying && _patrolModule.Owner != null
                    ? _patrolModule.Center
                    : ResolvePreviewCenter();

            DrawHorizontalCircle(
                center,
                _patrolModule.Radius,
                Color.white);

            if (!Application.isPlaying ||
                !_patrolModule.HasPatrolPoints)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(
                _patrolModule.PointA,
                PointMarkerRadius);

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(
                _patrolModule.PointB,
                PointMarkerRadius);

            if (_patrolModule.Owner == null)
                return;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(
                _patrolModule.Owner.position,
                _patrolModule.CurrentPoint);
        }

        private Vector3 ResolvePreviewCenter()
        {
            Rigidbody ownerBody =
                GetComponentInParent<Rigidbody>();

            return ownerBody != null
                ? ownerBody.transform.position
                : transform.position;
        }

        private static void DrawHorizontalCircle(
            Vector3 p_center,
            float p_radius,
            Color p_color)
        {
            Gizmos.color = p_color;
            Vector3 previousPoint =
                p_center + Vector3.right * p_radius;

            for (int index = 1; index <= CircleSegments; index++)
            {
                float angle = index / (float)CircleSegments *
                              Mathf.PI * 2f;

                Vector3 nextPoint = p_center +
                                    new Vector3(
                                        Mathf.Cos(angle),
                                        0f,
                                        Mathf.Sin(angle)) * p_radius;

                Gizmos.DrawLine(previousPoint, nextPoint);
                previousPoint = nextPoint;
            }
        }
    }
}
