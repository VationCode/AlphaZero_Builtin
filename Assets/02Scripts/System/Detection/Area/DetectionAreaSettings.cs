using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Alpha.Detection
{
    // 공용 탐지가 검색할 공간 형태를 구분한다.
    public enum EDetectionAreaShape
    {
        ForwardBox = 0,
        ForwardSector = 1,
        Radial = 2
    }

    // Entity 종류와 관계없이 영역 형태와 Physics 검색 조건을 보관한다.
    [Serializable]
    public class DetectionAreaSettings
    {
        [SerializeField]
        private EDetectionAreaShape _shape =
            EDetectionAreaShape.ForwardBox;

        [SerializeField]
        private Vector3 _localOffset = new(0f, 0.9f, 0f);

        [Tooltip(
            "기준 방향에서 판정 영역과 Local Offset을 " +
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

        [FormerlySerializedAs("_targetMask")]
        [SerializeField]
        private LayerMask _targetLayers = ~0;

        public EDetectionAreaShape Shape => _shape;
        public Vector3 LocalOffset => _localOffset;
        public float YawOffset => _yawOffset;
        public float Width => _width;
        public float Length => _length;
        public float Radius => _radius;
        public float Angle => _angle;
        public float Height => _height;
        public LayerMask TargetLayers => _targetLayers;

        // 기준 위치에서 현재 영역이 수평으로 도달할 수 있는 최대 거리다.
        public float MaximumHorizontalReach =>
            CalculateMaximumHorizontalReach();

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

        // 모든 Shape 값을 보정해 형태 변경 후에도 안전한 설정을 유지한다.
        public virtual void Validate()
        {
            _yawOffset = Mathf.Clamp(_yawOffset, -180f, 180f);
            _width = Mathf.Max(0.01f, _width);
            _length = Mathf.Max(0.01f, _length);
            _radius = Mathf.Max(0.01f, _radius);
            _angle = Mathf.Clamp(_angle, 0.1f, 360f);
            _height = Mathf.Max(0.01f, _height);
        }

        private float CalculateMaximumHorizontalReach()
        {
            if (!IsValid)
                return 0f;

            Vector2 offset = new(
                _localOffset.x,
                _localOffset.z);

            switch (_shape)
            {
                case EDetectionAreaShape.ForwardBox:
                {
                    float maximumX =
                        Mathf.Abs(offset.x) + _width * 0.5f;

                    float maximumZ = Mathf.Max(
                        Mathf.Abs(offset.y),
                        Mathf.Abs(offset.y + _length));

                    return new Vector2(
                        maximumX,
                        maximumZ).magnitude;
                }

                case EDetectionAreaShape.ForwardSector:
                {
                    if (_angle >= 360f)
                        return offset.magnitude + _radius;

                    float offsetAngle = Mathf.Atan2(
                        offset.x,
                        offset.y) * Mathf.Rad2Deg;

                    float halfAngle = _angle * 0.5f;
                    float farthestAngle = Mathf.Clamp(
                        offsetAngle,
                        -halfAngle,
                        halfAngle);

                    float angleRadians =
                        farthestAngle * Mathf.Deg2Rad;

                    Vector2 farthestDirection = new(
                        Mathf.Sin(angleRadians),
                        Mathf.Cos(angleRadians));

                    return (offset +
                            farthestDirection * _radius)
                        .magnitude;
                }

                case EDetectionAreaShape.Radial:
                    return offset.magnitude + _radius;

                default:
                    return 0f;
            }
        }
    }
}
