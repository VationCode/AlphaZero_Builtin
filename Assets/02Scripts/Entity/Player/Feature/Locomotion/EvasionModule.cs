using System;
using Alpha.Combat;
using UnityEngine;

namespace Alpha.Player.Locomotion
{
    // 회피 행동의 실제 이동, Root Motion, 무적 적용과 정리를 담당한다.
    public sealed class EvasionModule
    {
        private LocomotionMoveModule _moveModule;
        private RootMotionModule _rootMotionModule;
        private DamageReceiverModule _damageReceiver;
        private Func<float> _getGroundVerticalVelocity;

        private EvasionSettings _currentSettings;
        private Vector3 _direction;
        private ELocomotionMode _locomotionMode;
        private float _elapsedTime;
        private bool _ownsRootMotion;
        private bool _ownsInvulnerability;

        public bool IsActive { get; private set; }
        public EEvasionType CurrentType { get; private set; }
        public float ElapsedTime => _elapsedTime;
        public float Duration => _currentSettings?.Duration ?? 0f;
        public bool IsComplete =>
            IsActive && _elapsedTime >= Duration;

        private bool IsBound =>
            _moveModule != null &&
            _rootMotionModule != null &&
            _damageReceiver != null &&
            _getGroundVerticalVelocity != null;

        public bool Bind(
            LocomotionMoveModule p_moveModule,
            RootMotionModule p_rootMotionModule,
            DamageReceiverModule p_damageReceiver,
            Func<float> p_getGroundVerticalVelocity)
        {
            End();

            _moveModule = p_moveModule;
            _rootMotionModule = p_rootMotionModule;
            _damageReceiver = p_damageReceiver;
            _getGroundVerticalVelocity = p_getGroundVerticalVelocity;

            return IsBound;
        }

        // State가 결정한 회피 종류와 월드 방향으로 하나의 실행 세션을 시작한다.
        public bool Begin(
            EEvasionType p_type,
            EvasionSettings p_settings,
            Vector3 p_direction,
            ELocomotionMode p_locomotionMode)
        {
            if (!IsBound ||
                IsActive ||
                p_settings == null ||
                !p_settings.IsValid ||
                !TryNormalizeDirection(
                    p_direction,
                    p_locomotionMode,
                    out Vector3 direction))
            {
                return false;
            }

            bool usesRootMotion =
                p_settings.MovementMode != EEvasionMovementMode.Scripted;

            if (usesRootMotion &&
                !_rootMotionModule.Begin(
                    this,
                    ResolveRootMotionMode(p_settings.MovementMode)))
            {
                return false;
            }

            CurrentType = p_type;
            _currentSettings = p_settings;
            _direction = direction;
            _locomotionMode = p_locomotionMode;
            _elapsedTime = 0f;
            _ownsRootMotion = usesRootMotion;
            IsActive = true;

            UpdateInvulnerability();
            return true;
        }

        // Script 이동과 무적 시간을 갱신하고 행동 완료 여부를 반환한다.
        public bool Tick(float p_deltaTime)
        {
            if (!IsActive || _currentSettings == null)
                return false;

            float deltaTime = Mathf.Max(0f, p_deltaTime);
            float remainingTime = Mathf.Max(
                0f,
                _currentSettings.Duration - _elapsedTime);
            float activeTime = Mathf.Min(deltaTime, remainingTime);

            if (_currentSettings.MovementMode ==
                EEvasionMovementMode.Scripted)
            {
                ApplyScriptedMovement(activeTime);
            }

            _elapsedTime += activeTime;
            UpdateInvulnerability();
            return IsComplete;
        }

        // 강제 상태 전환에서도 자신이 획득한 기능만 정리한다.
        public void End()
        {
            if (_ownsInvulnerability && _damageReceiver != null)
                _damageReceiver.EndInvulnerability(this);

            if (_ownsRootMotion && _rootMotionModule != null)
                _rootMotionModule.End(this);

            IsActive = false;
            CurrentType = default;
            _currentSettings = null;
            _direction = Vector3.zero;
            _locomotionMode = default;
            _elapsedTime = 0f;
            _ownsRootMotion = false;
            _ownsInvulnerability = false;
        }

        private void ApplyScriptedMovement(float p_activeTime)
        {
            if (_moveModule == null || p_activeTime <= 0f)
                return;

            float speed =
                _currentSettings.Distance / _currentSettings.Duration;
            Vector3 moveDelta = _direction * speed * p_activeTime;

            if (_locomotionMode == ELocomotionMode.Ground)
            {
                moveDelta.y =
                    _getGroundVerticalVelocity() * p_activeTime;
            }

            _moveModule.MoveDelta(moveDelta);
        }

        private void UpdateInvulnerability()
        {
            if (_damageReceiver == null || _currentSettings == null)
                return;

            bool shouldBeInvulnerable =
                _currentSettings.InvulnerabilityDuration > 0f &&
                _elapsedTime >=
                    _currentSettings.InvulnerabilityStartTime &&
                _elapsedTime <
                    _currentSettings.InvulnerabilityEndTime;

            if (shouldBeInvulnerable && !_ownsInvulnerability)
            {
                _ownsInvulnerability =
                    _damageReceiver.BeginInvulnerability(this);
            }
            else if (!shouldBeInvulnerable && _ownsInvulnerability)
            {
                _damageReceiver.EndInvulnerability(this);
                _ownsInvulnerability = false;
            }
        }

        private static bool TryNormalizeDirection(
            Vector3 p_direction,
            ELocomotionMode p_mode,
            out Vector3 p_normalizedDirection)
        {
            p_normalizedDirection = p_mode == ELocomotionMode.Flight
                ? p_direction
                : Vector3.ProjectOnPlane(p_direction, Vector3.up);

            if (p_normalizedDirection.sqrMagnitude < 0.0001f)
                return false;

            p_normalizedDirection.Normalize();
            return true;
        }

        private static ERootMotionMode ResolveRootMotionMode(
            EEvasionMovementMode p_mode)
        {
            return p_mode switch
            {
                EEvasionMovementMode.RootMotionGround =>
                    ERootMotionMode.Ground,
                EEvasionMovementMode.RootMotionFull =>
                    ERootMotionMode.Full,
                _ => ERootMotionMode.None
            };
        }
    }
}
