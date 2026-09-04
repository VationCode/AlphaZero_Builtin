using UnityEngine;

namespace Alpha.Item.Weapon.View
{
    // Range 무기별 손 Grip과 상체 자세 표현 설정을 소유한다.
    [DisallowMultipleComponent]
    public sealed class RangeWeaponRigView : WeaponView
    {
        [Header("Left Hand Grip")]
        [SerializeField]
        private Transform _leftHandGrip;

        [SerializeField, Range(0f, 1f)]
        private float _leftHandPositionWeight = 1f;

        [SerializeField, Range(0f, 1f)]
        private float _leftHandRotationWeight = 1f;

        [Header("Range Upper Body Pose")]
        [SerializeField]
        [Tooltip("Range 무기를 장착한 동안 전투 입력이 없어도 상체 총기 자세를 유지합니다.")]
        private bool _holdPoseWhileEquipped = true;

        // 오른손은 Player의 WeaponHolder가 기준이 되고 왼손만 이 Grip을 추적한다.
        public Transform LeftHandGrip => _leftHandGrip;
        public float LeftHandPositionWeight => _leftHandPositionWeight;
        public float LeftHandRotationWeight => _leftHandRotationWeight;
        public bool HoldPoseWhileEquipped => _holdPoseWhileEquipped;

        private void OnValidate()
        {
            _leftHandPositionWeight =
                Mathf.Clamp01(_leftHandPositionWeight);
            _leftHandRotationWeight =
                Mathf.Clamp01(_leftHandRotationWeight);
        }
    }
}
