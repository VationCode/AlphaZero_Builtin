using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Alpha.Item.Weapon.Range
{
    // 공격 전달 방식과 무관한 한 번의 발사 간격·탄도 수·분산을 보관한다.
    [Serializable]
    public sealed class RangeShotSettings
    {
        [Tooltip("발사 사이의 시간(초)입니다. 초당 3발은 약 0.333초입니다.")]
        [SerializeField, Min(0.01f)]
        private float _fireInterval = 0.2f;

        [FormerlySerializedAs("_projectilesPerShot")]
        [Tooltip("한 번의 발사 요청에서 생성할 탄도 수입니다.")]
        [SerializeField, Range(1, 64)]
        private int _trajectoryCount = 1;

        [Tooltip("조준 방향을 중심으로 적용할 최대 분산 각도입니다.")]
        [SerializeField, Range(0f, 45f)]
        private float _spreadAngle;

        public float FireInterval => Mathf.Max(0.01f, _fireInterval);
        public int TrajectoryCount => Mathf.Clamp(_trajectoryCount, 1, 64);
        public float SpreadAngle => Mathf.Clamp(_spreadAngle, 0f, 45f);

        public void Validate()
        {
            _fireInterval = Mathf.Max(0.01f, _fireInterval);
            _trajectoryCount = Mathf.Clamp(_trajectoryCount, 1, 64);
            _spreadAngle = Mathf.Clamp(_spreadAngle, 0f, 45f);
        }
    }
}
