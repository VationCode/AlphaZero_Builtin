using UnityEngine;

namespace Alpha.Item.Weapon.Range
{
    // RangeWeapon이 선택한 실제 공격 방식을 실행하는 기반 Module이다.
    public abstract class RangeAttackModule : MonoBehaviour
    {
        // 충돌점이 없을 때 Camera Ray가 사용할 기본 조준 거리를 반환한다.
        public virtual float GetDefaultAimDistance(
            float p_maxDistance)
        {
            return p_maxDistance;
        }

        // 공격 방식에 맞춰 총구에서 시작할 최종 발사 방향을 계산한다.
        public virtual bool TryResolveLaunchDirection(
            Vector3 p_origin,
            Vector3 p_targetPoint,
            out Vector3 p_direction)
        {
            Vector3 targetDirection =
                p_targetPoint - p_origin;

            if (targetDirection.sqrMagnitude <= 0.0001f)
            {
                p_direction = Vector3.zero;
                return false;
            }

            p_direction = targetDirection.normalized;
            return true;
        }

        // 공통 요청 검증 후 구체 공격 방식에 실행을 위임한다.
        public bool TryExecute(
            in RangeAttackRequest p_request,
            out RangeAttackResult p_result)
        {
            p_result = default;

            return p_request.IsValid &&
                   OnExecute(
                       p_request,
                       out p_result);
        }

        // Hitscan과 Projectile이 각각 실제 공격을 구현한다.
        protected abstract bool OnExecute(
            in RangeAttackRequest p_request,
            out RangeAttackResult p_result);
    }
}
