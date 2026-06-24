using UnityEngine;

public class MouseUtility : MonoBehaviour
{
    [SerializeField]
    private Transform _mouseAim;

    public void ActivateCursor(bool p_isActivate)
    {
        if (!p_isActivate)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public Vector3 GetTargetMouseDirection(Vector2 p_mousePos, Vector3 p_playerPos)
    {
        Vector3 targetPos = GetMouseWorldPosition(p_mousePos);

        Vector3 dir = targetPos - p_playerPos;
        dir.y = 0f;

        return dir.normalized;
    }

    // QuarterView에서만 호출, 마우스 클릭시에만 적용
    public Vector3 GetMouseWorldPosition(Vector2 p_mousePos)
    {
         Ray ray = Camera.main.ScreenPointToRay(p_mousePos);

        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            _mouseAim.transform.position = hit.point;
            return hit.point;
        }

        return Vector3.zero;
    }
}
