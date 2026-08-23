using Alpha.Combat;
using UnityEngine;

namespace Alpha.Enemy
{
    // 외부 충격을 Rigidbody의 수평 이동으로 변환한다.
    [DisallowMultipleComponent]
    public sealed class EnemyKnockbackModule : MonoBehaviour, IKnockbackable
    {
        private const float DirectionEpsilon = 0.0001f;

        [SerializeField]
        private Rigidbody _rigidbody;

        private Transform _owner;
        private bool _canReceiveKnockback = true;
        private bool _isActive;
        private bool _hasLoggedMissingRigidbody;
        private Vector3 _velocity;
        private float _remainingTime;

        public bool IsActive => _isActive;
        public bool CanApplyKnockback =>
            isActiveAndEnabled &&
            _rigidbody != null &&
            !_rigidbody.isKinematic;
        public bool CanReceiveKnockback =>
            _canReceiveKnockback && CanApplyKnockback;

        // 대표 Locomotion Module이 Enemy 경계와 공용 Rigidbody를 연결한다.
        public void Bind(
            Transform p_owner,
            Rigidbody p_rigidbody)
        {
            _owner = p_owner;
            _rigidbody ??= p_rigidbody != null
                ? p_rigidbody
                : p_owner != null
                    ? p_owner.GetComponent<Rigidbody>()
                    : GetComponentInParent<Rigidbody>();

            _canReceiveKnockback = true;
            Cancel();
        }

        public bool TryApplyKnockback(
            in KnockbackInfo p_knockbackInfo)
        {
            if (!CanReceiveKnockback ||
                !p_knockbackInfo.IsValid ||
                !TryGetRigidbody(out Rigidbody body))
            {
                return false;
            }

            Vector3 direction = Vector3.ProjectOnPlane(
                p_knockbackInfo.Direction,
                Vector3.up);

            if (direction.sqrMagnitude <= DirectionEpsilon &&
                _owner != null)
            {
                direction = Vector3.ProjectOnPlane(
                    _owner.position -
                    p_knockbackInfo.Attacker.position,
                    Vector3.up);
            }

            if (direction.sqrMagnitude <= DirectionEpsilon)
                return false;

            _velocity =
                direction.normalized *
                (p_knockbackInfo.Distance /
                 p_knockbackInfo.Duration);
            _remainingTime = p_knockbackInfo.Duration;
            _isActive = true;

            SetHorizontalVelocity(body, _velocity);
            return true;
        }

        // 마지막 물리 프레임의 시간 비율까지 반영해 설정 거리를 맞춘다.
        public bool Tick(float p_deltaTime)
        {
            if (!_isActive)
                return false;

            if (_remainingTime <= 0f)
            {
                Cancel();
                return false;
            }

            if (!TryGetRigidbody(out Rigidbody body) ||
                body.isKinematic)
            {
                ClearState();
                return false;
            }

            float fixedDeltaTime = Mathf.Max(
                Mathf.Epsilon,
                p_deltaTime);
            float activeTime = Mathf.Min(
                _remainingTime,
                fixedDeltaTime);

            SetHorizontalVelocity(
                body,
                _velocity *
                (activeTime / fixedDeltaTime));

            _remainingTime = Mathf.Max(
                0f,
                _remainingTime - activeTime);

            return true;
        }

        // 사망한 Enemy가 새로운 외부 충격을 받지 않도록 수신 상태를 관리한다.
        public void SetEnabled(bool p_enabled)
        {
            _canReceiveKnockback = p_enabled;

            if (!p_enabled)
                Cancel();
        }

        public void Cancel()
        {
            ClearState();

            if (_rigidbody != null)
                StopHorizontalMovement(_rigidbody);
        }

        private void ClearState()
        {
            _isActive = false;
            _velocity = Vector3.zero;
            _remainingTime = 0f;
        }

        private bool TryGetRigidbody(out Rigidbody p_body)
        {
            p_body = _rigidbody;

            if (p_body != null)
                return true;

            if (!_hasLoggedMissingRigidbody)
            {
                Debug.LogError(
                    $"[{name}] Knockback에 사용할 Rigidbody가 필요합니다.",
                    this);
                _hasLoggedMissingRigidbody = true;
            }

            return false;
        }

        private static void StopHorizontalMovement(Rigidbody p_body)
        {
            Vector3 velocity = p_body.linearVelocity;
            velocity.x = 0f;
            velocity.z = 0f;
            p_body.linearVelocity = velocity;
        }

        private static void SetHorizontalVelocity(
            Rigidbody p_body,
            Vector3 p_horizontalVelocity)
        {
            Vector3 velocity = p_body.linearVelocity;
            velocity.x = p_horizontalVelocity.x;
            velocity.z = p_horizontalVelocity.z;
            p_body.linearVelocity = velocity;
        }

        private void OnDisable()
        {
            Cancel();
        }

        private void OnValidate()
        {
            _rigidbody ??= GetComponentInParent<Rigidbody>();
        }
    }
}
