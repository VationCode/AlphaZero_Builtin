using UnityEngine;

namespace Alpha.Player.Combat
{
    // Camera 조준 Ray로 공격 방식이 사용할 월드 목표점을 결정한다.
    public class RangeAimModule : MonoBehaviour
    {
        [Header("Aim Collision")]
        [SerializeField]
        private LayerMask _aimMask;

        [Header("Scope Attack")]
        [Tooltip("Scope에서 Camera Near Plane 앞쪽을 공격 시작점으로 사용할 거리입니다.")]
        [SerializeField, Min(0f)]
        private float _scopeSpawnOffset = 0.05f;

        private PlayerCore _core;

        public bool Bind(PlayerCore p_core)
        {
            if (p_core == null)
                return false;

            _core = p_core;
            return true;
        }

        public bool TryResolveAttackPose(
            Vector3 p_muzzleOrigin,
            float p_maxDistance,
            out Vector3 p_attackOrigin,
            out Vector3 p_targetPoint)
        {
            p_attackOrigin = Vector3.zero;
            p_targetPoint = Vector3.zero;

            if (_core?.CameraCore?.RenderCamera == null ||
                p_maxDistance <= 0f ||
                !TryCreateViewRay(out Ray viewRay))
            {
                return false;
            }

            bool isScope =
                _core.CameraCore.Context.EffectiveViewType == ECameraViewType.Scope;

            p_attackOrigin = 
                isScope? viewRay.GetPoint(_scopeSpawnOffset) : p_muzzleOrigin;

            // Scope 중앙 발사점까지 총구가 막혔다면 엄폐물 관통을 방지한다.
            if (isScope && Physics.Linecast(p_muzzleOrigin, p_attackOrigin, _aimMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            // Camera와 실제 발사점의 간격만큼 조준 Ray의 검사 거리를 보정한다.
            float originOffset = Vector3.Distance(viewRay.origin, p_attackOrigin);

            float viewRayDistance = p_maxDistance + originOffset;

            p_targetPoint = viewRay.GetPoint(viewRayDistance);

            if (Physics.Raycast(viewRay, out RaycastHit hit, viewRayDistance, _aimMask, QueryTriggerInteraction.Ignore))
            {
                p_targetPoint = hit.point;
            }

            return (p_targetPoint - p_attackOrigin).sqrMagnitude > 0.0001f;
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

                p_viewRay = renderCamera.ScreenPointToRay(_core.Input.MouseInputPos);

                return true;
            }

            // TPS, Aim, Scope에서는 화면 중앙을 조준점으로 사용한다.
            p_viewRay = renderCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));

            return true;
        }
    }
}
