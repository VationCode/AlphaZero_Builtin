using System;
using UnityEngine;

namespace Alpha.Item.Weapon.Range
{
    // 관통 Hitscan 공격의 투영 영역 설정만 소유한다.
    [Serializable]
    public sealed class PenetrationAttackSettings : RangeAttackSettings
    {
        [Tooltip("관통 영역이 발사 지점에서 가질 반경입니다.")]
        [SerializeField, Min(0.01f)]
        private float _startRadius = 0.25f;

        [Tooltip("관통 영역이 최대 거리 지점에서 가질 반경입니다.")]
        [SerializeField, Min(0.01f)]
        private float _endRadius = 0.25f;

        public override ERangeAttackType AttackType =>
            ERangeAttackType.Penetration;
        public override bool IsValid =>
            _startRadius > 0f && _endRadius > 0f;
        public float StartRadius => Mathf.Max(0.01f, _startRadius);
        public float EndRadius => Mathf.Max(0.01f, _endRadius);

        public override void Validate()
        {
            _startRadius = Mathf.Max(0.01f, _startRadius);
            _endRadius = Mathf.Max(0.01f, _endRadius);
        }
    }
}
