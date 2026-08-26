using System;
using UnityEngine;

namespace Alpha.Detection
{
    // 공용 탐지가 검색할 공간 형태를 구분한다.
    public enum EDetectionAreaShape
    {
        ForwardBox = 0,
        ForwardSector = 1,
        Radial = 2
    }

    // Entity 종류와 관계없이 공간 탐지에 필요한 형태와 Physics 조건을 보관한다.
    [Serializable]
    public class DetectionAreaSettings
    {
        [SerializeField]
        private EDetectionAreaShape _shape =
            EDetectionAreaShape.ForwardBox;

        [SerializeField]
        private Vector3 _localOffset = new(0f, 0.9f, 0f);

        [Tooltip(
            "공격자의 정면을 기준으로 판정 영역과 Local Offset을 " +
            "회전할 Y축 각도입니다.")]
        [SerializeField, Range(-180f, 180f)]
        private float _yawOffset;

        [SerializeField, Min(0.01f)]
        private float _width = 2f;

        [SerializeField, Min(0.01f)]
        private float _length = 2.5f;

        [SerializeField, Min(0.01f)]
        private float _radius = 2.5f;

        [SerializeField, Range(0.1f, 360f)]
        private float _angle = 90f;

        [SerializeField, Min(0.01f)]
        private float _height = 1.8f;

        [SerializeField]
        private LayerMask _targetMask = ~0;

        [SerializeField]
        private QueryTriggerInteraction _triggerInteraction =
            QueryTriggerInteraction.Ignore;

        public EDetectionAreaShape Shape => _shape;
        public Vector3 LocalOffset => _localOffset;
        public float YawOffset => _yawOffset;
        public float Width => _width;
        public float Length => _length;
        public float Radius => _radius;
        public float Angle => _angle;
        public float Height => _height;
        public LayerMask TargetMask => _targetMask;
        public QueryTriggerInteraction TriggerInteraction =>
            _triggerInteraction;

        public bool IsValid => _shape switch
        {
            EDetectionAreaShape.ForwardBox =>
                _width > 0f && _length > 0f && _height > 0f,

            EDetectionAreaShape.ForwardSector =>
                _radius > 0f && _angle > 0f && _height > 0f,

            EDetectionAreaShape.Radial =>
                _radius > 0f && _height > 0f,

            _ => false
        };

        // 직렬화된 잘못된 수치가 Physics 판정으로 전달되지 않게 보정한다.
        public void Validate()
        {
            _yawOffset = Mathf.Clamp(_yawOffset, -180f, 180f);
            _width = Mathf.Max(0.01f, _width);
            _length = Mathf.Max(0.01f, _length);
            _radius = Mathf.Max(0.01f, _radius);
            _angle = Mathf.Clamp(_angle, 0.1f, 360f);
            _height = Mathf.Max(0.01f, _height);
        }
    }
}
