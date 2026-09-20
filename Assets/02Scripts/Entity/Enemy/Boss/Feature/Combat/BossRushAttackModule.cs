using UnityEngine;

namespace Alpha.Boss
{
    // 확정한 방향과 곡선을 Rigidbody 속도로 실행하고, 사용한 물리 설정을 복구한다.
    public sealed class BossRushAttackModule
    {
        private Rigidbody _body;
        private bool _originalUseGravity;
        private float _originalLinearDamping;
        private bool _controlsHeight;

        public bool IsConfigured(BossMovementAttackSettings p_settings) =>
            p_settings != null &&
            (p_settings.DirectionType == EBossMovementDirectionType.Forward ||
             p_settings.DirectionType == EBossMovementDirectionType.Target) &&
            IsFinite(p_settings.Distance) && p_settings.Distance >= 0f &&
            IsFinite(p_settings.Height) && p_settings.Height >= 0f &&
            IsFinite(p_settings.StartTimeSeconds) && p_settings.StartTimeSeconds >= 0f &&
            IsFinite(p_settings.DurationSeconds) && p_settings.DurationSeconds >= 0.01f &&
            p_settings.MovementCurve != null && p_settings.MovementCurve.length > 0 &&
            p_settings.HeightCurve != null && p_settings.HeightCurve.length > 0;

        public bool Begin(Rigidbody p_body, Transform p_target,
            BossMovementAttackSettings p_settings, BossRushAttackContext p_context,
            AnimationCurve p_rootMotionCurve = null)
        {
            if (_body != null || p_body == null || p_body.isKinematic ||
                !IsConfigured(p_settings) || p_context == null || p_context.Started ||
                (p_settings.DirectionType == EBossMovementDirectionType.Target && p_target == null))
                return false;

            Vector3 direction = p_settings.DirectionType == EBossMovementDirectionType.Target
                ? p_target.position - p_body.position : p_body.transform.forward;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = p_body.transform.forward;
                direction.y = 0f;
            }
            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector3.forward;
            if (!IsFinite(direction))
                return false;

            _body = p_body;
            _originalUseGravity = _body.useGravity;
            _originalLinearDamping = _body.linearDamping;
            _controlsHeight = p_settings.Height > 0f;
            // 지상 돌진은 중력과 낙하 속도를 유지하고, 점프만 높이 곡선이 Y를 제어한다.
            if (_controlsHeight)
                _body.useGravity = false;
            _body.linearDamping = 0f;

            p_context.Clear();
            p_context.RootMotionCurve = p_settings.UseRootMotion ? p_rootMotionCurve : null;
            p_context.Started = true;
            p_context.StartPosition = _body.position;
            p_context.Direction = direction.normalized;
            _body.MoveRotation(Quaternion.LookRotation(p_context.Direction, Vector3.up));
            return true;
        }

        // FixedUpdate에서만 호출한다. 마지막 속도는 다음 물리 스텝까지 유지한다.
        public bool Tick(BossMovementAttackSettings p_settings, BossRushAttackContext p_context,
            float p_stepSeconds, float p_fixedDeltaTime)
        {
            if (_body == null || _body.isKinematic || p_context == null || !p_context.Started ||
                !IsConfigured(p_settings) || !IsFinite(p_fixedDeltaTime) || p_fixedDeltaTime <= 0f ||
                !IsFinite(p_stepSeconds) || p_stepSeconds < 0f)
                return false;

            if (p_context.ElapsedSeconds >= p_settings.DurationSeconds)
            {
                // 직전 FixedUpdate의 마지막 이동이 물리 시뮬레이션에 반영된 뒤 정지한다.
                Stop();
                p_context.Completed = true;
                return true;
            }

            float elapsed = Mathf.Min(p_context.ElapsedSeconds + p_stepSeconds, p_settings.DurationSeconds);
            float progress = elapsed / p_settings.DurationSeconds;
            // 전체 루트 이동량으로 정규화한 진행률에 Distance를 곱한다. 마지막에 남은 거리를 몰아서 더하지 않는다.
            AnimationCurve movementCurve = p_context.RootMotionCurve ?? p_settings.MovementCurve;
            float distanceRatio = progress >= 1f ? 1f : movementCurve.Evaluate(progress);
            float heightRatio = progress >= 1f ? 0f : p_settings.HeightCurve.Evaluate(progress);
            Vector3 offset = p_context.Direction * (p_settings.Distance * distanceRatio) +
                Vector3.up * (p_settings.Height * heightRatio);
            Vector3 displacement = offset - p_context.PreviousOffset;
            Vector3 correction = p_context.StartPosition + p_context.PreviousOffset - _body.position;
            if (!_controlsHeight)
                correction.y = 0f;
            // 접촉 마찰의 오차는 보정하되, 벽에 막혀도 보정량은 한 스텝 이동량을 넘지 않는다.
            displacement += Vector3.ClampMagnitude(correction, displacement.magnitude);
            Vector3 velocity = displacement / p_fixedDeltaTime;
            if (!_controlsHeight)
                velocity.y = _body.linearVelocity.y;
            if (!IsFinite(velocity))
                return false;

            _body.linearVelocity = velocity;
            p_context.ElapsedSeconds = elapsed;
            p_context.PreviousOffset = offset;
            return true;
        }

        public void Stop()
        {
            if (_body != null)
            {
                if (!_body.isKinematic)
                {
                    Vector3 velocity = _body.linearVelocity;
                    // 점프 취소 시 공중에서 멈춘 뒤 원래 중력으로 낙하한다.
                    _body.linearVelocity = new Vector3(0f, _controlsHeight ? 0f : velocity.y, 0f);
                }
                _body.useGravity = _originalUseGravity;
                _body.linearDamping = _originalLinearDamping;
            }
            _body = null;
            _controlsHeight = false;
        }

        private static bool IsFinite(float p_value) => !float.IsNaN(p_value) && !float.IsInfinity(p_value);
        private static bool IsFinite(Vector3 p_value) =>
            IsFinite(p_value.x) && IsFinite(p_value.y) && IsFinite(p_value.z);
    }
}
