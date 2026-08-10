using System;
using UnityEngine;

namespace Alpha.Combat
{
    // 공격이 검색할 공간 형태를 구분한다.
    public enum EAttackAreaShape
    {
        ForwardBox,
        ForwardSector,
        Radial
    }

    // 무기와 스킬이 Inspector에서 조정하는 공용 범위 설정이다.
    [Serializable]
    public sealed class AttackAreaSettings
    {
        [SerializeField]
        private EAttackAreaShape _shape = EAttackAreaShape.ForwardBox;

        [SerializeField]
        private Vector3 _localOffset = new(0f, 0.9f, 0f);

        [Header("Forward Box")]
        [SerializeField, Min(0.01f)]
        private float _width = 2f;

        [SerializeField, Min(0.01f)]
        private float _length = 2.5f;

        [Header("Sector / Radial")]
        [SerializeField, Min(0.01f)]
        private float _radius = 2.5f;

        [SerializeField, Range(0.1f, 360f)]
        private float _angle = 90f;

        [Header("Common")]
        [SerializeField, Min(0.01f)]
        private float _height = 1.8f;

        [SerializeField]
        private LayerMask _targetMask = ~0;

        [SerializeField]
        private QueryTriggerInteraction _triggerInteraction =
            QueryTriggerInteraction.Ignore;

        public EAttackAreaShape Shape => _shape;
        public Vector3 LocalOffset => _localOffset;
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
            EAttackAreaShape.ForwardBox =>
                _width > 0f && _length > 0f && _height > 0f,

            EAttackAreaShape.ForwardSector =>
                _radius > 0f && _angle > 0f && _height > 0f,

            EAttackAreaShape.Radial =>
                _radius > 0f && _height > 0f,

            _ => false
        };

        // 직렬화된 잘못된 수치가 Physics 판정으로 전달되지 않게 보정한다.
        public void Validate()
        {
            _width = Mathf.Max(0.01f, _width);
            _length = Mathf.Max(0.01f, _length);
            _radius = Mathf.Max(0.01f, _radius);
            _angle = Mathf.Clamp(_angle, 0.1f, 360f);
            _height = Mathf.Max(0.01f, _height);
        }
    }
}
