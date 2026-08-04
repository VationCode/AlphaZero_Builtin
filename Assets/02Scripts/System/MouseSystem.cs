using UnityEngine;

namespace Alpha.Mouse
{
    public class MouseSystem : MonoBehaviour
    {
        [Header("World Raycast")]
        [SerializeField] private LayerMask _worldMask;
        [SerializeField] private float _rayDistance = 1000f;

        private Camera _renderCamera;

        private bool _isViewCursorActive;
        private bool _isUICursorActive;
        // 마우스 좌표 계산에 사용할 Camera를 연결한다.
        public void Bind(Camera p_renderCamera)
        {
            _renderCamera = p_renderCamera;
        }

        // Camera View가 필요한 커서 상태를 설정한다.
        public void SetViewCursor(bool p_isActive)
        {
            _isViewCursorActive = p_isActive;
            ApplyCursor();
        }
        // UI가 필요한 커서 상태를 설정한다.
        public void SetUICursor(bool p_isActive)
        {
            _isUICursorActive = p_isActive;
            ApplyCursor();
        }

        // 현재 요청 상태에 맞춰 커서를 적용한다.
        private void ApplyCursor()
        {
            bool isActive =
                _isViewCursorActive ||
                _isUICursorActive;

            Cursor.lockState = isActive
                ? CursorLockMode.None
                : CursorLockMode.Locked;

            Cursor.visible = isActive;
        }

        // 화면의 마우스 좌표를 월드 좌표로 변환한다.
        public bool TryGetWorldPoint(Vector2 p_screenPosition, out Vector3 p_worldPoint)
        {
            Ray ray = _renderCamera.ScreenPointToRay(p_screenPosition);

            if (Physics.Raycast(ray, out RaycastHit hit, _rayDistance, _worldMask, QueryTriggerInteraction.Ignore))
            {
                p_worldPoint = hit.point;
                return true;
            }

            p_worldPoint = default;
            return false;
        }

        // 기준 위치에서 마우스 월드 좌표 방향을 계산한다.
        public bool TryGetWorldDirection(Vector2 p_screenPosition, Vector3 p_origin, out Vector3 p_direction)
        {
            if (!TryGetWorldPoint(p_screenPosition, out Vector3 worldPoint))
            {
                p_direction = default;
                return false;
            }

            p_direction = worldPoint - p_origin;
            p_direction.y = 0f;
            p_direction.Normalize();

            return true;
        }


        // QuarterView에서만 호출, 마우스 클릭시에만 적용
        /*public Vector3 GetTargetMouseDirection(Vector2 p_mousePos, Vector3 p_playerPos)
        {
            Vector3 targetPos = GetMouseWorldPosition(p_mousePos);

            Vector3 dir = targetPos - p_playerPos;
            dir.y = 0f;

            return dir.normalized;
        }

        private Vector3 GetMouseWorldPosition(Vector2 p_mousePos)
        {
            Ray ray = Camera.main.ScreenPointToRay(p_mousePos);

            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                _mouseAim.transform.position = hit.point;
                return hit.point;
            }

            return Vector3.zero;
        }*/
    }
}