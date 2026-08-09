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
        private Vector3 _direction;

        private float _damage;
        private float _speed;
        private float _remainingDistance;

        private bool _isActive;

        // 발사 순간의 공격 정보를 투사체 런타임 상태로 복사한다.
        public bool Initialize(
            in RangeAttackRequest p_request,
            float p_speed)
        {
            if (!p_request.IsValid || p_speed <= 0f)
            {
                return false;
            }

            _attacker = p_request.Attacker;
            _direction = p_request.Direction;
            _damage = p_request.Damage;
            _speed = p_speed;
            _remainingDistance = p_request.MaxDistance;

            transform.SetPositionAndRotation(
                p_request.Origin,
                Quaternion.LookRotation(_direction));

            _isActive = true;
            return true;
        }

        private void Update()
        {
            if (!_isActive)
                return;

            float moveDistance = Mathf.Min(
                _speed * Time.deltaTime,
                _remainingDistance);

            if (moveDistance <= 0f)
            {
                Release();
                return;
            }

            Ray moveRay = new(transform.position, _direction);

            if (Physics.Raycast(
                    moveRay,
                    out RaycastHit hit,
                    moveDistance,
                    _hitMask,
                    _triggerInteraction))
            {
                HandleHit(hit);
                return;
            }

            transform.position += _direction * moveDistance;

            _remainingDistance -= moveDistance;

            if (_remainingDistance <= 0f)
                Release();
        }

        private void HandleHit(RaycastHit p_hit)
        {
            transform.position = p_hit.point;

            DamageInfo damageInfo = new(
                _attacker,
                _damage,
                p_hit.point,
                p_hit.normal,
                _direction);

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
