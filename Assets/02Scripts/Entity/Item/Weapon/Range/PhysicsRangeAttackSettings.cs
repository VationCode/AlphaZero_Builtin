using System;
using UnityEngine;

namespace Alpha.Item.Weapon.Range
{
    // 즉시 Physics 판정 공격들이 공유하는 충돌 정보만 보관한다.
    [Serializable]
    public sealed class PhysicsRangeAttackSettings
    {
        [Tooltip("공격 판정에 포함할 Layer입니다.")]
        [SerializeField]
        private LayerMask _hitMask = (LayerMask)129;

        public LayerMask HitMask => _hitMask;
    }
}
