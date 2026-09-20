using System;
using Alpha.Combat;
using Alpha.Detection;
using UnityEngine;

namespace Alpha.Boss
{
    public enum EBossDamageTiming { Automatic, DuringMovement, MovementEnd, AnimationWindow }

    // 이전 Scene·Prefab의 피해 설정을 읽기 위한 호환 데이터다. 새 설정은 Profile과 DirectHit로 분리한다.
    [Serializable]
    public sealed class BossDamageSettings
    {
        [SerializeField] private DamageProfile _profile = new();
        [SerializeField] private bool _enabled = true;
        [Tooltip("Automatic: Melee는 애니메이션 구간, 지상 Rush는 이동 중, 점프 Rush는 이동 완료 시 판정합니다.")]
        [SerializeField] private EBossDamageTiming _timing;
        [Tooltip("보스 위치와 수평 방향을 기준으로 한 월드 단위 영역입니다. 모델의 크기와 별도로 설정합니다.")]
        [SerializeField] private DetectionAreaSettings _area = new();
        [SerializeField, Min(0f)] private float _startTimeSeconds = 0.2f;
        [SerializeField, Min(0f)] private float _endTimeSeconds = 0.6f;

        public DamageProfile Profile => _profile;
        public bool Enabled => _enabled;
        public EBossDamageTiming Timing => _timing;
        public DetectionAreaSettings Area => _area;
        public float StartTimeSeconds => _startTimeSeconds;
        public float EndTimeSeconds => _endTimeSeconds;

    }
}

