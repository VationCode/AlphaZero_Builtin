using Alpha.Combat;
using Alpha.Item.Weapon.Range;
using UnityEngine;

namespace Alpha.Projectile
{
    // 발사 후 이동, 충돌, 피해 전달, 사거리 종료를 관리한다.
    public class Projectile : MonoBehaviour
    {
        [Header("Collision")]
        [SerializeField]
        private LayerMask _hitMask;

        [SerializeField]
        private QueryTriggerInteraction _triggerInteraction =
            QueryTriggerInteraction.Ignore;

        private Transform _attacker;
        private Vector3 _velocity;
        private Vector3 _gravity;

        private float _damage;
        private float _collisionRadius;
        private float _remainingDistance;

        private bool _isActive;

        // 발사 순간의 공격 정보를 투사체 런타임 상태로 복사한다.
        public bool Initialize(
            in RangeAttackRequest p_request,
            Vector3 p_initialVelocity,
            Vector3 p_gravity,
            float p_collisionRadius)
        {
            if (!p_request.IsValid ||
                p_initialVelocity.sqrMagnitude <= 0.0001f ||
                p_collisionRadius < 0f)
            {
                return false;
            }

            _attacker = p_request.Attacker;
            _velocity = p_initialVelocity;
            _gravity = p_gravity;
            _damage = p_request.Damage;
            _collisionRadius = p_collisionRadius;
            _remainingDistance = p_request.MaxDistance;

            transform.SetPositionAndRotation(
                p_request.Origin,
                Quaternion.LookRotation(
                    _velocity.normalized));

            _isActive = true;
            return true;
        }

        private void Update()
        {
            if (!_isActive)
                return;

            float deltaTime = Time.deltaTime;

            Vector3 displacement =
                _velocity * deltaTime +
                0.5f * _gravity *
                deltaTime * deltaTime;

            float requestedDistance =
                displacement.magnitude;

            float moveDistance = Mathf.Min(
                requestedDistance,
                _remainingDistance);

            if (moveDistance <= 0f ||
                requestedDistance <= 0.0001f)
            {
                Release();
                return;
            }

            Vector3 moveDirection =
                displacement / requestedDistance;

            Ray moveRay = new(
                transform.position,
                moveDirection);

            if (TryCastMovement(
                    moveRay,
                    moveDistance,
                    out RaycastHit hit))
            {
                HandleHit(
                    hit,
                    moveDirection);
                return;
            }

            transform.position +=
                moveDirection * moveDistance;

            _remainingDistance -= moveDistance;

            if (_remainingDistance <= 0f)
            {
                Release();
                return;
            }

            _velocity +=
                _gravity * deltaTime;

            if (_velocity.sqrMagnitude > 0.0001f)
            {
                transform.rotation =
                    Quaternion.LookRotation(
                        _velocity.normalized);
            }
        }

        private bool TryCastMovement(
            Ray p_moveRay,
            float p_moveDistance,
            out RaycastHit p_hit)
        {
            if (_collisionRadius > 0f)
            {
                return Physics.SphereCast(
                    p_moveRay,
                    _collisionRadius,
                    out p_hit,
                    p_moveDistance,
                    _hitMask,
                    _triggerInteraction);
            }

            return Physics.Raycast(
                p_moveRay,
                out p_hit,
                p_moveDistance,
                _hitMask,
                _triggerInteraction);
        }

        private void HandleHit(
            RaycastHit p_hit,
            Vector3 p_direction)
        {
            transform.position = p_hit.point;

            DamageInfo damageInfo = new(
                _attacker,
                _damage,
                p_hit.point,
                p_hit.normal,
                p_direction);

            DamageSystem.TryApply(p_hit.collider, damageInfo);

            // 지형이나 피해 불가능 대상에 명중해도 투사체는 종료한다.
            Release();
        }

        // 이후 ObjectPool을 적용할 때 이 메서드만 반환 처리로 교체한다.
        private void Release()
        {
            _isActive = false;
            Destroy(gameObject);
        }
    }
}
