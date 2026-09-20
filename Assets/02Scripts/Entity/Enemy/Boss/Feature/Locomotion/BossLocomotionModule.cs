using UnityEngine;

namespace Alpha.Boss
{
    // 사거리 조정의 수평 이동과 회전만 실행한다. 공격·후딜 판단과 Rush 이동은 소유하지 않는다.
    [DisallowMultipleComponent]
    public sealed class BossLocomotionModule : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _moveSpeed = 5f;
        [SerializeField, Min(0f)] private float _rotationSpeed = 180f;
        private Rigidbody _body;
        private bool _ownsMovement;

        public bool CanMove => isActiveAndEnabled && _body != null && !_body.isKinematic && _moveSpeed > 0f;

        public void Bind(Rigidbody p_body)
        {
            Stop();
            _body = p_body;
        }

        public bool FaceTarget(Vector3 p_target, float p_deltaTime)
        {
            if (_body == null)
                return true;
            Vector3 direction = p_target - _body.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                return true;
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            bool facing = Quaternion.Angle(_body.rotation, targetRotation) <= 5f;
            _body.MoveRotation(Quaternion.RotateTowards(_body.rotation, targetRotation,
                Mathf.Max(0f, _rotationSpeed) * p_deltaTime));
            return facing;
        }

        public void Approach(Vector3 p_target, float p_stopDistance, float p_deltaTime)
            => MoveToDistance(p_target, p_stopDistance, p_deltaTime, false);

        public void Retreat(Vector3 p_target, float p_stopDistance, float p_deltaTime)
            => MoveToDistance(p_target, p_stopDistance, p_deltaTime, true);

        private void MoveToDistance(Vector3 p_target, float p_stopDistance, float p_deltaTime, bool p_retreat)
        {
            if (!CanMove || p_deltaTime <= 0f)
            {
                Stop();
                return;
            }
            Vector3 offset = p_target - _body.position;
            offset.y = 0f;
            float remaining = p_retreat
                ? Mathf.Max(0f, p_stopDistance) - offset.magnitude
                : offset.magnitude - Mathf.Max(0f, p_stopDistance);
            if (remaining <= 0f)
            {
                Stop();
                return;
            }
            // 한 물리 스텝에 목표 사거리를 지나치지 않는다. 낙하 속도는 보존한다.
            Vector3 direction = offset.sqrMagnitude > 0.0001f ? offset.normalized : _body.rotation * Vector3.forward;
            if (p_retreat)
                direction = -direction;
            direction.y = 0f;
            Vector3 velocity = direction.normalized * Mathf.Min(_moveSpeed, remaining / p_deltaTime);
            velocity.y = _body.linearVelocity.y;
            _body.linearVelocity = velocity;
            _ownsMovement = true;
        }

        public void Stop()
        {
            // 제어권을 넘긴 뒤에는 Rush나 다른 물리 반응의 속도를 지우지 않는다.
            if (_ownsMovement && _body != null && !_body.isKinematic)
                _body.linearVelocity = new Vector3(0f, _body.linearVelocity.y, 0f);
            _ownsMovement = false;
        }

        private void OnDisable() => Stop();
    }
}
