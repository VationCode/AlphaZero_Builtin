using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Alpha.Utility
{
    // 설정한 수평 반경을 Scene 뷰에 표시한다.
    [DisallowMultipleComponent]
    [AddComponentMenu("Alpha/Utility/Radius Gizmo View")]
    public sealed class RadiusGizmoView : MonoBehaviour
    {
        [SerializeField, Min(0f)]
        private float _radius = 1f;

        [SerializeField]
        private Color _color = Color.cyan;

        private void OnValidate()
        {
            _radius = Mathf.Max(0f, _radius);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_radius <= 0f)
                return;

            Vector3 center = transform.position + Vector3.up * 0.1f;
            Color previousColor = Handles.color;

            Handles.color = _color;
            Handles.DrawWireDisc(center, Vector3.up, _radius);
            Handles.Label(
                center + Vector3.forward * _radius + Vector3.up * 0.3f,
                $"Radius: {_radius:0.##} m");

            Handles.color = previousColor;
        }
#endif
    }
}