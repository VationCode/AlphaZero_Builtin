using System;
using UnityEngine;

namespace Alpha.Enemy.CrabBoss.Combat
{
    [Serializable]
    public sealed class CrabBossRushAttackStrategy
        : CrabBossAttackStrategy
    {
        [SerializeField, Min(0f)]
        private float _stopDistance = 2f;

        [SerializeField, Min(0.01f)]
        private float _moveDuration = 1f;

        private CrabBossLocomotionModule _locomotion;
        private Vector3 _destination;
        private float _moveSpeed;

        public override ECrabAttackPattern Pattern =>
            ECrabAttackPattern.RushAttack;

        public override bool Begin(
            CrabBossContext p_context,
            CrabBossLocomotionModule p_locomotion)
        {
            Cancel();

            if (p_context == null ||
                !p_context.HasTarget ||
                p_locomotion == null ||
                p_locomotion.Owner == null)
            {
                return false;
            }

            Vector3 ownerPosition = p_locomotion.Owner.position;
            Vector3 direction =
                p_context.Target.position - ownerPosition;
            direction.y = 0f;

            float distance = direction.magnitude;
            float moveDistance = distance - _stopDistance;

            if (moveDistance <= 0f)
                return false;

            // 돌진 시작 시점의 플레이어 위치를 기준으로 목적지를 고정한다.
            _destination = ownerPosition +
                           direction.normalized * moveDistance;
            _destination.y = ownerPosition.y;
            _moveSpeed = moveDistance / _moveDuration;
            _locomotion = p_locomotion;
            IsComplete = false;

            return true;
        }

        public override void Tick(float p_deltaTime)
        {
            if (IsComplete || _locomotion == null)
                return;

            IsComplete = _locomotion.MoveTowards(
                _destination,
                _moveSpeed,
                p_deltaTime);
        }

        public override void Cancel()
        {
            base.Cancel();
            _locomotion = null;
        }
    }
}
