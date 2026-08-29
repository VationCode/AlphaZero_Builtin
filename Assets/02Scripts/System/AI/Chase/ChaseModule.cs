using UnityEngine;

namespace Alpha.AI
{
    // 추적 허용 영역과 추적 종료 후 복귀할 영역을 관리한다.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ChaseGizmoView))]
    public sealed class ChaseModule : MonoBehaviour
    {
        [Tooltip("추적 종료 후 복귀를 완료한 것으로 판단할 중심 반경입니다.")]
        [SerializeField, Min(0f)]
        private float _returnRadius = 10f;

        [Tooltip("중심에서 타깃을 추적할 수 있는 최대 반경입니다.")]
        [SerializeField, Min(0f)]
        private float _radius = 15f;

        private Transform _owner;
        private Vector3 _center;

        public Transform Owner => _owner;
        public Vector3 Center => _center;
        public float ReturnRadius => _returnRadius;
        public float Radius => _radius;

        public void Bind(Transform p_owner)
        {
            _owner = p_owner != null
                ? p_owner
                : ResolvePreviewOwner();

            _center = _owner != null
                ? _owner.position
                : transform.position;
        }

        public bool Contains(Vector3 p_position)
        {
            return HorizontalSqrDistance(
                       p_position,
                       _center) <=
                   _radius * _radius;
        }

        public bool IsInsideReturnArea(Vector3 p_position)
        {
            return HorizontalSqrDistance(
                       p_position,
                       _center) <=
                   _returnRadius * _returnRadius;
        }

        private Transform ResolvePreviewOwner()
        {
            Rigidbody ownerBody =
                GetComponentInParent<Rigidbody>();

            return ownerBody != null
                ? ownerBody.transform
                : transform;
        }

        private static float HorizontalSqrDistance(
            Vector3 p_from,
            Vector3 p_to)
        {
            Vector3 offset = p_to - p_from;
            offset.y = 0f;
            return offset.sqrMagnitude;
        }

        private void OnValidate()
        {
            _returnRadius = Mathf.Max(0f, _returnRadius);
            _radius = Mathf.Max(_returnRadius, _radius);
        }
    }
}
