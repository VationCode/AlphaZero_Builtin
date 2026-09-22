using System;
using UnityEngine;

namespace Alpha.Boss
{
    public enum EBossRushMovement { Forward, StationaryJump, TargetLeap }

    // Rush 타입의 이동 특성이다. 선택 거리는 Common 설정에서 독립적으로 정한다.
    [Serializable]
    public sealed class BossRushSettings
    {
        [SerializeField, Tooltip("직선 돌진·제자리 점프·타겟 도약 중 이동 방식을 선택합니다.")]
        private EBossRushMovement _movement;
        [SerializeField, Min(0f), Tooltip("이동 실행에 사용할 속도입니다. 패턴 선택 거리와는 별개입니다.")]
        private float _speed = 10f;
        [SerializeField, Min(0f), Tooltip("도약 방식에서 사용할 최고 높이입니다.")]
        private float _jumpHeight = 3f;

        public EBossRushMovement Movement => _movement;
        public float Speed => _speed;
        public float JumpHeight => _jumpHeight;

        internal void Validate()
        {
            if (!Enum.IsDefined(typeof(EBossRushMovement), _movement)) _movement = EBossRushMovement.Forward;
            _speed = BossPatternSettings.NonNegative(_speed);
            _jumpHeight = BossPatternSettings.NonNegative(_jumpHeight);
        }
    }
}
