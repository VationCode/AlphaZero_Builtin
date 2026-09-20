using UnityEngine;

namespace Alpha.Boss
{
    // Boss 전체 행동을 조정한다. 선택한 패턴의 사거리를 확보한 뒤 Combat에 실행을 요청한다.
    [DisallowMultipleComponent]
    public sealed class BossActionFlow : MonoBehaviour
    {
        [SerializeField] private bool _automaticAttacks = true;
        [SerializeField, Min(0f)]
        [Tooltip("접근·후퇴 시 사거리 안쪽으로 확보할 여유입니다. 패턴 거리 폭의 절반 이하로 제한됩니다.")]
        private float _approachMargin = 0.25f;
        [SerializeField, Min(0.1f)] private float _repositionTimeoutSeconds = 6f;
        [SerializeField, Min(0f)] private float _repositionRetrySeconds = 3f;

        private BossCore _boss;
        private BossCombatFlow _combat;
        private BossLocomotionModule _locomotion;
        private Transform _selectedTarget;
        private bool _isRepositioning;
        private float _desiredDistance;
        private float _repositionElapsed;
        private float _repositionRetryTime;
        public BossPatternData PendingPattern { get; private set; }

        public void Bind(BossCore p_boss, BossCombatFlow p_combat, BossLocomotionModule p_locomotion)
        {
            Unbind();
            _boss = p_boss;
            _combat = p_combat;
            _locomotion = p_locomotion;
            _repositionRetryTime = 0f;
            _combat.OnAttackStarted += CancelPendingAction;
            _combat.OnAttackCancelled += CancelPendingAction;
            _boss.EncounterContext.OnStateChanged += HandleEncounterState;
        }

        public void Unbind()
        {
            CancelPendingAction();
            if (_combat != null)
            {
                _combat.OnAttackStarted -= CancelPendingAction;
                _combat.OnAttackCancelled -= CancelPendingAction;
            }
            if (_boss != null)
                _boss.EncounterContext.OnStateChanged -= HandleEncounterState;
            _boss = null;
            _combat = null;
            _locomotion = null;
        }

        public void CancelPendingAction()
        {
            PendingPattern = null;
            _selectedTarget = null;
            _isRepositioning = false;
            _repositionElapsed = 0f;
            _locomotion?.Stop();
        }

        public void SetAutomaticAttacksEnabled(bool p_enabled)
        {
            _automaticAttacks = p_enabled;
            if (!p_enabled)
                CancelPendingAction();
        }

        private void HandleEncounterState(EBossEncounterState p_state)
        {
            if (p_state != EBossEncounterState.Combat)
            {
                CancelPendingAction();
                _repositionRetryTime = 0f;
            }
        }

        private void FixedUpdate()
        {
            if (!_automaticAttacks || _boss == null || !_boss.isActiveAndEnabled ||
                _boss.HealthContext.IsDead || _boss.EncounterContext.CurrentState != EBossEncounterState.Combat ||
                _boss.Target == null || _combat == null || !_combat.isActiveAndEnabled ||
                _locomotion == null || !_locomotion.isActiveAndEnabled ||
                _boss.CombatContext.State != EBossCombatState.Idle)
            {
                CancelPendingAction();
                return;
            }
            if (Time.fixedDeltaTime <= 0f)
                return;

            Vector3 position = _boss.Rigidbody != null ? _boss.Rigidbody.position : _boss.transform.position;
            Vector3 offset = _boss.Target.position - position;
            offset.y = 0f;
            float distance = offset.magnitude;
            if (_selectedTarget != _boss.Target || (PendingPattern != null &&
                !_combat.CanKeepActionPattern(PendingPattern, distance, _locomotion.CanMove)))
                CancelPendingAction();

            if (PendingPattern == null)
            {
                bool canReposition = _locomotion.CanMove && Time.time >= _repositionRetryTime;
                if (!_combat.TrySelectActionPattern(distance, canReposition, out BossPatternData pattern))
                {
                    _locomotion.Stop();
                    return;
                }
                PendingPattern = pattern;
                _selectedTarget = _boss.Target;
            }

            BossPatternSelectionSettings selection = PendingPattern.Selection;
            bool inRange = selection.CanSelectAtDistance(distance);
            float margin = Mathf.Min(Mathf.Max(0f, _approachMargin),
                (selection.MaximumDistance - selection.MinimumDistance) * 0.5f);
            bool facing = _locomotion.FaceTarget(_selectedTarget.position, Time.fixedDeltaTime);

            if (!inRange)
            {
                _isRepositioning = true;
                _desiredDistance = distance < selection.MinimumDistance
                    ? selection.MinimumDistance + margin : selection.MaximumDistance - margin;
            }
            if (_isRepositioning && _locomotion.CanMove &&
                (!inRange || Mathf.Abs(distance - _desiredDistance) > Mathf.Min(0.01f, margin)))
            {
                _repositionElapsed += Time.fixedDeltaTime;
                if (_repositionElapsed >= Mathf.Max(0.1f, _repositionTimeoutSeconds))
                {
                    // 벽이나 계속 이동하는 타겟 때문에 같은 공격 준비에 갇히지 않는다.
                    CancelPendingAction();
                    _repositionRetryTime = Time.time + Mathf.Max(0f, _repositionRetrySeconds);
                    return;
                }
                if (distance < _desiredDistance)
                    _locomotion.Retreat(_selectedTarget.position, _desiredDistance, Time.fixedDeltaTime);
                else
                    _locomotion.Approach(_selectedTarget.position, _desiredDistance, Time.fixedDeltaTime);
                return;
            }

            _isRepositioning = false;
            _locomotion.Stop();
            // 회전 중 거리가 바뀌어도 공격 직전에 최소·최대 거리 조건을 다시 확인한다.
            if (facing && inRange)
            {
                BossPatternData pattern = PendingPattern;
                CancelPendingAction();
                _combat.TryStartAttack(pattern);
            }
        }

        private void OnDisable() => CancelPendingAction();
        private void OnDestroy() => Unbind();
    }
}
