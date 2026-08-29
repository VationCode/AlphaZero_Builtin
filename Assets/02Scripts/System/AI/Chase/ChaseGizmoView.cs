using UnityEngine;

namespace Alpha.AI
{
    // 복귀 완료 반경과 최대 추적 반경을 Scene Gizmo로 표현한다.
    [DisallowMultipleComponent]
    public sealed class ChaseGizmoView : MonoBehaviour
    {
        private const int CircleSegments = 48;

        [SerializeField]
        private ChaseModule _chaseModule;

        private void Reset()
        {
            _chaseModule = GetComponent<ChaseModule>();
        }

        private void OnValidate()
        {
            _chaseModule ??= GetComponent<ChaseModule>();
        }

        private void OnDrawGizmosSelected()
        {
            _chaseModule ??= GetComponent<ChaseModule>();

            if (_chaseModule == null)
                return;

            Vector3 center =
                Application.isPlaying && _chaseModule.Owner != null
                    ? _chaseModule.Center
                    : ResolvePreviewCenter();

            DrawHorizontalCircle(
                center,
                _chaseModule.ReturnRadius,
                Color.gray);

            DrawHorizontalCircle(
                center,
                _chaseModule.Radius,
                Color.yellow);
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
