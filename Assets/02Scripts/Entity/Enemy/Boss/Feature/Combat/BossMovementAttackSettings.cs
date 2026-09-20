using System;
using UnityEngine;

namespace Alpha.Boss
{
    // 이동 시작 시 수평 방향을 결정할 기준이다.
    public enum EBossMovementDirectionType
    {
        Forward = 0,
        Target = 1
    }

    // 이동 공격의 경로와 시간을 보관한다. 거리 0은 제자리, 높이 0은 지상 이동을 뜻한다.
    [Serializable]
    public sealed class BossMovementAttackSettings
    {
        private const float MinimumDuration = 0.01f;

        [SerializeField]
        [Tooltip("Forward는 보스 전방, Target은 이동 시작 시점의 타겟 방향입니다.")]
        private EBossMovementDirectionType _directionType = EBossMovementDirectionType.Forward;

        [SerializeField, Min(0f)]
        [Tooltip("선택한 방향의 최종 수평 이동거리입니다. Root Motion을 사용해도 이 거리를 유지합니다. 0이면 제자리이며 장애물에 막히면 실제 이동은 짧아질 수 있습니다.")]
        private float _distance;

        [SerializeField, Min(0f)]
        [Tooltip("시작 위치를 기준으로 한 점프 높이입니다. 높이 곡선 값에 곱하며, 0이면 지상 이동입니다.")]
        private float _height;

        [SerializeField, Min(0f)]
        [Tooltip("공격 애니메이션 시작 후 이동을 시작할 시간(초)입니다.")]
        private float _startTimeSeconds;

        [SerializeField, Min(MinimumDuration)]
        [Tooltip("이동 시작부터 완료까지 걸리는 시간(초)입니다.")]
        private float _durationSeconds = 1f;

        [SerializeField]
        [Tooltip("Rush의 애니메이션 이동 구간을 샘플링하여 수평 이동 리듬을 사용합니다. 최종 거리는 Distance이며, 루트 이동이 없으면 Movement Curve를 사용합니다. 높이는 Height 설정을 유지합니다.")]
        private bool _useRootMotion;

        [SerializeField]
        [Tooltip("Root Motion을 끄거나 추출할 수 없을 때 사용합니다. X는 시간 진행률(0~1), Y는 수평 거리 진행률이며 시작 0, 끝 1로 설정합니다.")]
        private AnimationCurve _movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [SerializeField]
        [Tooltip("X는 이동 시간 진행률(0~1), Y는 점프 높이 비율입니다. 시작과 끝은 0, 최고점은 1로 설정합니다.")]
        private AnimationCurve _heightCurve = CreateDefaultHeightCurve();

        public EBossMovementDirectionType DirectionType => _directionType;
        public float Distance => _distance;
        public float Height => _height;
        public float StartTimeSeconds => _startTimeSeconds;
        public float DurationSeconds => _durationSeconds;
        public bool UseRootMotion => _useRootMotion;
        public AnimationCurve MovementCurve => _movementCurve;
        public AnimationCurve HeightCurve => _heightCurve;

        public void Validate()
        {
            if (!Enum.IsDefined(typeof(EBossMovementDirectionType), _directionType))
                _directionType = EBossMovementDirectionType.Forward;

            _distance = Mathf.Max(0f, _distance);
            _height = Mathf.Max(0f, _height);
            _startTimeSeconds = Mathf.Max(0f, _startTimeSeconds);
            _durationSeconds = Mathf.Max(MinimumDuration, _durationSeconds);

            if (_movementCurve == null || _movementCurve.length == 0)
                _movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

            if (_heightCurve == null || _heightCurve.length == 0)
                _heightCurve = CreateDefaultHeightCurve();
        }

        private static AnimationCurve CreateDefaultHeightCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.5f, 1f),
                new Keyframe(1f, 0f));
        }
    }
}
