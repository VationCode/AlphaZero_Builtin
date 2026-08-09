using UnityEngine;

namespace Alpha.Player.Combat
{
    // Camera 조준 Ray를 총구 기준의 실제 발사 방향으로 변환한다.
    public class RangeAimModule : MonoBehaviour
    {
        [Header("Aim Collision")]
        [SerializeField]
        private LayerMask _aimMask;

        private PlayerCore _core;

        public bool Bind(PlayerCore p_core)
        {
            if (p_core == null)
                return false;

            _core = p_core;
            return true;
        }

        public bool TryResolveDirection(
            Vector3 p_origin,
            float p_maxDistance,
            out Vector3 p_direction)
        {
            p_direction = Vector3.zero;

            if (_core?.CameraCore?.RenderCamera == null ||
                p_maxDistance <= 0f ||
                !TryCreateViewRay(out Ray viewRay))
            {
                return false;
            }

            // Camera와 총구의 간격만큼 조준 Ray의 검사 거리를 보정한다.
            float viewRayDistance =
                p_maxDistance +
                Vector3.Distance(viewRay.origin, p_origin);

            Vector3 targetPoint =
                viewRay.GetPoint(viewRayDistance);

            if (Physics.Raycast(
                    viewRay,
                    out RaycastHit hit,
                    viewRayDistance,
                    _aimMask,
                    QueryTriggerInteraction.Ignore))
            {
                targetPoint = hit.point;
            }

            Vector3 attackDirection =
                targetPoint - p_origin;

            if (attackDirection.sqrMagnitude <= 0.0001f)
                return false;

            p_direction = attackDirection.normalized;
            return true;
        }

        private bool TryCreateViewRay(out Ray p_viewRay)
        {
            Camera renderCamera =
                _core.CameraCore.RenderCamera;

            ECameraViewType viewType =
                _core.CameraCore.Context.EffectiveViewType;

            // Quarter에서는 현재 마우스 위치를 조준점으로 사용한다.
            if (viewType == ECameraViewType.Quarter)
            {
                if (_core.Input == null)
                {
                    p_viewRay = default;
                    return false;
                }

                p_viewRay = renderCamera.ScreenPointToRay(
                    _core.Input.MouseInputPos);

                return true;
            }

            // TPS, Aim, Scope에서는 화면 중앙을 조준점으로 사용한다.
            p_viewRay = renderCamera.ViewportPointToRay(
                new Vector3(0.5f, 0.5f));

            return true;
        }
    }
}
