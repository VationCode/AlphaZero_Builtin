using UnityEngine;

namespace Alpha.Item.Weapon.Range
{
    // RangeWeapon의 사용자, 조준 자세, 마지막 발사 결과 상태를 소유한다.
    internal sealed class RangeWeaponContext
    {
        private RangeWeaponUseContext _useContext;
        private RangeWeaponAttackPose _attackPose;

        public bool HasUser => _useContext.IsValid;
        public Transform Attacker => _useContext.Attacker;
        public float AdditionalDamage => _useContext.AdditionalDamage;
        public Vector3 LastFireDirection { get; private set; }

        public bool BindUser(in RangeWeaponUseContext p_context)
        {
            if (!p_context.IsValid)
                return false;

            _useContext = p_context;
            _attackPose = default;
            LastFireDirection = Vector3.zero;
            return true;
        }

        public void ClearUser()
        {
            _useContext = default;
            _attackPose = default;
            LastFireDirection = Vector3.zero;
        }

        public bool SetAttackPose(in RangeWeaponAttackPose p_pose)
        {
            if (!HasUser || !p_pose.IsValid)
            {
                _attackPose = default;
                return false;
            }

            _attackPose = p_pose;
            return true;
        }

        public void ClearAttackPose()
        {
            _attackPose = default;
        }

        public bool TryGetAttackPose(
            out Vector3 p_origin,
            out Vector3 p_direction)
        {
            p_origin = _attackPose.Origin;
            p_direction = _attackPose.Direction;
            return HasUser && _attackPose.IsValid;
        }

        public void SetLastFireDirection(Vector3 p_direction)
        {
            LastFireDirection = p_direction.sqrMagnitude > 0.0001f
                ? p_direction.normalized
                : Vector3.zero;
        }
    }
}
