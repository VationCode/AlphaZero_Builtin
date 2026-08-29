using Alpha.AI;
using UnityEngine;

namespace Alpha.Enemy
{
    // 일반 이동·회전을 실행하고 순찰·넉백 기능은 전용 Module에 위임한다.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ChaseModule))]
    [RequireComponent(typeof(EnemyKnockbackModule))]
    public sealed class EnemyLocomotionModule : MonoBehaviour
    {
        private const float DirectionEpsilon = 0.0001f;
        private const float RotationCompleteAngle = 0.1f;

        [Header("Physics")]
        [SerializeField]
        private Rigidbody _rigidbody;

        [SerializeField]
        private PatrolModule _patrolModule;

        [SerializeField]
        private ChaseModule _chaseModule;

        [SerializeField]
        private EnemyKnockbackModule _knockbackModule;

        [Header("Movement")]
        [SerializeField, Min(0f)]
        private float _moveSpeed = 3f;

        [SerializeField, Min(0f)]
        private float _rotationSpeed = 360f;

        [SerializeField, Min(0f)]
        private float _arrivalDistance = 0.05f;

        private Transform _owner;
        private bool _hasLoggedMissingRigidbody;

        public Vector3 ReturnCenter =>
            _chaseModule != null
                ? _chaseModule.Center
                : transform.position;
        public bool UsesPatrol =>
            _patrolModule?.UsePatrol == true;
        public bool IsKnockbackActive =>
            _knockbackModule?.IsActive == true;
        public bool CanReceiveKnockback =>
            _knockbackModule?.CanReceiveKnockback == true;
        public bool CanApplyKnockback =>
            _knockbackModule?.CanApplyKnockback == true;

        public void Bind(Transform p_owner)
        {
            _owner = p_owner;
            _patrolModule ??= GetComponent<PatrolModule>();
            _chaseModule ??= GetComponent<ChaseModule>();
            _knockbackModule ??= GetComponent<EnemyKnockbackModule>();
            _rigidbody ??= p_owner != null
                ? p_owner.GetComponent<Rigidbody>()
                : GetComponentInParent<Rigidbody>();

            ValidateRigidbody();
            _patrolModule?.Bind(p_owner);
            _chaseModule?.Bind(p_owner);
            _knockbackModule?.Bind(p_owner, _rigidbody);
        }

        // Flow가 Patrol 상태일 때 현재 순찰 지점을 향해 이동한다.
        public void TickPatrol(float p_deltaTime)
        {
            if (IsKnockbackActive ||
                !UsesPatrol ||
                _patrolModule == null ||
                !_patrolModule.HasPatrolPoints)
            {
                return;
            }

            if (!MoveTo(
                    _patrolModule.CurrentPoint,
                    p_deltaTime))
            {
                return;
            }

            _patrolModule.AdvancePoint();
        }

        // 현재 위치에서 목적지까지 수평 속도를 적용하고 진행 방향으로 회전한다.
        public bool MoveTo(
            Vector3 p_destination,
            float p_deltaTime)
        {
            return MoveTo(
                p_destination,
                p_deltaTime,
                _moveSpeed);
        }

        // Rush처럼 패턴이 이동 속도를 소유할 때 지정 속도로 이동한다.
        public bool MoveTo(
            Vector3 p_destination,
            float p_deltaTime,
            float p_moveSpeed)
        {
            if (IsKnockbackActive)
                return false;

            if (!TryGetRigidbody(out Rigidbody body))
                return false;

            Vector3 direction = Vector3.ProjectOnPlane(
                p_destination - body.position,
                Vector3.up);
            float arrivalDistance = Mathf.Max(0f, _arrivalDistance);

            if (direction.sqrMagnitude <=
                arrivalDistance * arrivalDistance)
            {
                StopHorizontalMovement(body);
                return true;
            }

            float deltaTime = Mathf.Max(0f, p_deltaTime);
            float distance = direction.magnitude;
            Vector3 moveDirection = direction / distance;

            RotateDirection(body, moveDirection, deltaTime);

            float moveSpeed = Mathf.Max(0f, p_moveSpeed);

            if (deltaTime > 0f)
                moveSpeed = Mathf.Min(moveSpeed, distance / deltaTime);

            Vector3 velocity = body.linearVelocity;
            velocity.x = moveDirection.x * moveSpeed;
            velocity.z = moveDirection.z * moveSpeed;
            body.linearVelocity = velocity;

            return false;
        }

        // 위치는 변경하지 않고 목적지 방향으로만 회전한다.
        public bool RotateTo(
            Vector3 p_destination,
            float p_deltaTime)
        {
            if (IsKnockbackActive)
                return false;

            if (!TryGetRigidbody(out Rigidbody body))
                return false;

            return RotateDirection(
                body,
                Vector3.ProjectOnPlane(
                    p_destination - body.position,
                    Vector3.up),
                p_deltaTime);
        }

        public void Stop()
        {
            // 일반 행동의 정지 요청이 진행 중인 넉백 속도를 지우지 않게 한다.
            if (IsKnockbackActive)
                return;

            if (TryGetRigidbody(out Rigidbody body))
                StopHorizontalMovement(body);
        }

        // 상위 Flow가 사용하는 API는 유지하고 실제 처리는 Knockback Module에 위임한다.
        public void SetKnockbackEnabled(bool p_enabled)
        {
            _knockbackModule?.SetEnabled(p_enabled);
        }

        public void CancelKnockback()
        {
            _knockbackModule?.Cancel();
        }

        public bool TickKnockback(float p_deltaTime)
        {
            return _knockbackModule?.Tick(p_deltaTime) == true;
        }

        // 대상 또는 소유자가 최대 추적 영역을 벗어났는지 반환한다.
        public bool IsOutsideChaseArea(Vector3 p_position)
        {
            return _chaseModule == null ||
                   !_chaseModule.Contains(p_position);
        }

        // 복귀 상태가 추적 중심으로 충분히 돌아왔는지 판단할 때 사용한다.
        public bool IsInsideReturnArea(Vector3 p_position)
        {
            return _chaseModule == null ||
                   _chaseModule.IsInsideReturnArea(p_position);
        }

        private bool RotateDirection(
            Rigidbody p_body,
            Vector3 p_direction,
            float p_deltaTime)
        {
            if (p_direction.sqrMagnitude <= DirectionEpsilon)
                return true;

            Quaternion targetRotation = Quaternion.LookRotation(
                p_direction.normalized,
                Vector3.up);

            Quaternion nextRotation = Quaternion.RotateTowards(
                p_body.rotation,
                targetRotation,
                Mathf.Max(0f, _rotationSpeed) *
                Mathf.Max(0f, p_deltaTime));

            p_body.MoveRotation(nextRotation);

            return Quaternion.Angle(
                       nextRotation,
                       targetRotation) <=
                   RotationCompleteAngle;
        }

        private bool TryGetRigidbody(out Rigidbody p_body)
        {
            p_body = _rigidbody;

            if (p_body != null)
                return true;

            if (!_hasLoggedMissingRigidbody)
            {
                Debug.LogError(
                    $"[{name}] Enemy 루트에 Rigidbody가 필요합니다.",
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

        private void ValidateRigidbody()
        {
            if (_rigidbody == null)
                return;

            if (!_rigidbody.useGravity || _rigidbody.isKinematic)
            {
                Debug.LogWarning(
                    $"[{name}] Ground 이동에는 Use Gravity가 켜진 " +
                    "Dynamic Rigidbody가 필요합니다.",
                    _rigidbody);
            }

            RigidbodyConstraints requiredConstraints =
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationZ;

            if ((_rigidbody.constraints & requiredConstraints) !=
                requiredConstraints)
            {
                Debug.LogWarning(
                    $"[{name}] 지면 충돌로 넘어지지 않도록 Rigidbody의 " +
                    "Rotation X/Z 고정을 권장합니다.",
                    _rigidbody);
            }
        }

        private void OnValidate()
        {
            _moveSpeed = Mathf.Max(0f, _moveSpeed);
            _rotationSpeed = Mathf.Max(0f, _rotationSpeed);
            _arrivalDistance = Mathf.Max(0f, _arrivalDistance);

            if (_rigidbody == null && _owner != null)
                _rigidbody = _owner.GetComponent<Rigidbody>();

            _patrolModule ??= GetComponent<PatrolModule>();
            _chaseModule ??= GetComponent<ChaseModule>();
            _knockbackModule ??= GetComponent<EnemyKnockbackModule>();
        }
    }
}
