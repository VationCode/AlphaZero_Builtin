using UnityEngine;

namespace Alpha.Test.Combat
{
    using Alpha.Combat;

    // 실제 AttackAreaSystem 결과를 Scene Gizmo로 확인하는 테스트 전용 View다.
    public sealed class AttackAreaTestView : MonoBehaviour
    {
        [Header("Origin")]
        [SerializeField]
        private Transform _origin;

        [SerializeField]
        private Transform _attackerRoot;

        [Header("Area")]
        [SerializeField]
        private AttackAreaSettings _settings = new();

        [Header("Test")]
        [SerializeField]
        private bool _queryContinuously;

        [SerializeField, Min(1)]
        private int _bufferCapacity = 64;

        [SerializeField]
        private Color _areaColor = new(1f, 0.75f, 0f, 1f);

        [SerializeField]
        private Color _hitColor = Color.red;

        [SerializeField, Min(0.01f)]
        private float _hitMarkerRadius = 0.15f;

        private Collider[] _overlapBuffer;
        private AttackAreaHit[] _hitBuffer;
        private int _hitCount;

        public int LastHitCount => _hitCount;

        private Transform Origin => _origin != null
            ? _origin
            : transform;

        private Transform AttackerRoot => _attackerRoot != null
            ? _attackerRoot
            : transform;

        private void Awake()
        {
            // 테스트 View는 편집 모드 전용이며 실제 Play 판정에는 참여하지 않는다.
            if (Application.isPlaying)
            {
                enabled = false;
                return;
            }

            EnsureBuffers();
        }

        private void Update()
        {
            if (_queryContinuously)
                ExecuteQuery(false);
        }

        private void OnValidate()
        {
            _bufferCapacity = Mathf.Max(1, _bufferCapacity);
            _hitMarkerRadius = Mathf.Max(0.01f, _hitMarkerRadius);
            _settings?.Validate();
            EnsureBuffers();
        }

        [ContextMenu("Test Attack Area")]
        public void TestArea()
        {
            if (Application.isPlaying)
                return;

            ExecuteQuery(true);
        }

        [ContextMenu("Clear Test Result")]
        public void ClearResult()
        {
            _hitCount = 0;

            if (_hitBuffer != null && _hitBuffer.Length > 0)
                _hitBuffer[0] = default;
        }

        private void ExecuteQuery(bool p_logResult)
        {
            if (_settings == null)
                return;

            EnsureBuffers();
            Physics.SyncTransforms();

            Transform origin = Origin;

            AttackAreaRequest request = new(
                origin.position,
                origin.forward,
                origin.up,
                AttackerRoot,
                _settings);

            _hitCount = AttackAreaSystem.Query(
                request,
                _overlapBuffer,
                _hitBuffer);

            if (p_logResult)
                LogResult();
        }

        private void EnsureBuffers()
        {
            int capacity = Mathf.Max(1, _bufferCapacity);

            if (_overlapBuffer == null ||
                _overlapBuffer.Length != capacity)
            {
                _overlapBuffer = new Collider[capacity];
            }

            if (_hitBuffer == null ||
                _hitBuffer.Length != capacity)
            {
                _hitBuffer = new AttackAreaHit[capacity];
                _hitCount = 0;
            }
        }

        private void LogResult()
        {
            Debug.Log(
                $"Attack Area Test: {_settings.Shape}, " +
                $"Hit Count: {_hitCount}",
                this);

            for (int index = 0; index < _hitCount; index++)
            {
                AttackAreaHit hit = _hitBuffer[index];
                string targetName = hit.Target != null
                    ? hit.Target.name
                    : hit.Collider.name;

                Debug.Log(
                    $"[{index}] {targetName} / " +
                    $"Point: {hit.HitPoint}",
                    hit.Collider);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (Application.isPlaying || _settings == null)
                return;

            Transform origin = Origin;

            AttackAreaRequest request = new(
                origin.position,
                origin.forward,
                origin.up,
                AttackerRoot,
                _settings);

            AttackAreaGizmoDrawer.Draw(
                request,
                _areaColor);

            if (_hitBuffer == null)
                return;

            Color previousColor = Gizmos.color;
            Gizmos.color = _hitColor;

            for (int index = 0;
                 index < _hitCount && index < _hitBuffer.Length;
                 index++)
            {
                AttackAreaHit hit = _hitBuffer[index];

                if (!hit.IsValid)
                    continue;

                Gizmos.DrawWireSphere(
                    hit.HitPoint,
                    _hitMarkerRadius);

                Gizmos.DrawLine(
                    request.AreaOrigin,
                    hit.HitPoint);
            }

            Gizmos.color = previousColor;
        }
    }
}
