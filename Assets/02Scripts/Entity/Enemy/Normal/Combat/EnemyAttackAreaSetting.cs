using System;
using Alpha.Detection;
using UnityEngine;

namespace Alpha.Enemy
{
    // Enemy 공격 판정의 공간 정보와 애니메이션 활성 구간을 함께 보관한다.
    [Serializable]
    public sealed class EnemyAttackAreaSetting : DetectionAreaSettings
    {
        private const float DefaultActiveDuration = 0.1f;

        [Tooltip("공격 애니메이션 시작 후 Area 판정을 활성화할 시간입니다.")]
        [SerializeField, Min(0f)]
        private float _activationTimeSeconds;

        [Tooltip("공격 애니메이션 시작 후 Area 판정을 비활성화할 시간입니다.")]
        [SerializeField, Min(0f)]
        private float _deactivationTimeSeconds = DefaultActiveDuration;

        public float ActivationTimeSeconds =>
            Mathf.Max(0f, _activationTimeSeconds);

        public float DeactivationTimeSeconds =>
            _deactivationTimeSeconds > ActivationTimeSeconds
                ? _deactivationTimeSeconds
                : ActivationTimeSeconds + DefaultActiveDuration;

        public bool IsExecutable =>
            IsValid && DeactivationTimeSeconds > ActivationTimeSeconds;

        public bool IsActive(float p_elapsedSeconds)
        {
            float elapsedSeconds = Mathf.Max(0f, p_elapsedSeconds);
            return elapsedSeconds >= ActivationTimeSeconds &&
                   elapsedSeconds < DeactivationTimeSeconds;
        }

        public override void Validate()
        {
            base.Validate();

            _activationTimeSeconds = Mathf.Max(
                0f,
                _activationTimeSeconds);
            _deactivationTimeSeconds = Mathf.Max(
                _activationTimeSeconds + DefaultActiveDuration,
                _deactivationTimeSeconds);
        }
    }
}
