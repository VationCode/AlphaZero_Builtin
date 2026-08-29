using System;
using UnityEngine;

namespace Alpha.Item.Weapon.Range
{
    // 공격 방식과 무관한 원거리 무기의 공통 발사 감각을 보관한다.
    [Serializable]
    public sealed class RangeAttackTuning
    {
        [Tooltip("발사 사이의 시간(초)입니다. 초당 3발은 약 0.333초입니다.")]
        [SerializeField, Min(0.01f)]
        private float _fireInterval = 0.2f;

        [Tooltip("한 번의 발사 요청에서 생성할 탄도 수입니다.")]
        [SerializeField, Range(1, 64)]
        private int _projectilesPerShot = 1;

        [Tooltip("조준 방향을 중심으로 적용할 최대 분산 각도입니다.")]
        [SerializeField, Range(0f, 45f)]
        private float _spreadAngle;

        [Tooltip("한 발을 발사할 때 사용할 반동 세기입니다.")]
        [SerializeField, Min(0f)]
        private float _recoil;

        [Tooltip("Range 전투 방향을 사용하는 동안 적용할 이동속도 배율입니다.")]
        [SerializeField, Range(0f, 1f)]
        private float _moveSpeedMultiplier = 1f;

        public float FireInterval =>
            Mathf.Max(0.01f, _fireInterval);
        public int ProjectilesPerShot =>
            Mathf.Clamp(_projectilesPerShot, 1, 64);
        public float SpreadAngle =>
            Mathf.Clamp(_spreadAngle, 0f, 45f);
        public float Recoil => Mathf.Max(0f, _recoil);
        public float MoveSpeedMultiplier =>
            Mathf.Clamp01(_moveSpeedMultiplier);
        public void Validate()
        {
            _fireInterval = Mathf.Max(0.01f, _fireInterval);
            _projectilesPerShot = Mathf.Clamp(
                _projectilesPerShot,
                1,
                64);
            _spreadAngle = Mathf.Clamp(_spreadAngle, 0f, 45f);
            _recoil = Mathf.Max(0f, _recoil);
            _moveSpeedMultiplier = Mathf.Clamp01(
                _moveSpeedMultiplier);
        }
    }
}
