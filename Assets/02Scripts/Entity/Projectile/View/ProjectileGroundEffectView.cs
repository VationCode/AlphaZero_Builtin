using UnityEngine;

namespace Alpha.Projectile.View
{
    // Projectile 이동 경로를 따라 별도의 지면 One-shot 효과를 생성한다.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Projectile))]
    public sealed class ProjectileGroundEffectView : MonoBehaviour
    {
        [Tooltip("Projectile과 분리해서 생성할 비활성 Particle Effect 원본입니다.")]
        [SerializeField]
        private Transform _effectTemplate;

        [Tooltip("Projectile이 이 거리만큼 이동할 때마다 새 지면 효과를 생성합니다.")]
        [SerializeField, Min(0.01f)]
        private float _spawnDistance = 4f;

        [SerializeField]
        private LayerMask _groundMask = 1;

        [SerializeField, Min(0f)]
        private float _groundProbeHeight = 10f;

        [SerializeField, Min(0.01f)]
        private float _groundProbeDepth = 30f;

        [Tooltip("별도로 생성한 One-shot Effect를 제거하기까지의 시간입니다.")]
        [SerializeField, Min(0.01f)]
        private float _effectLifetime = 3f;

        private Vector3 _previousPosition;
        private float _distanceSinceSpawn;
        private bool _hasPreviousPosition;
        private Transform _leadingEffect;

        private void OnEnable()
        {
            if (_effectTemplate != null &&
                _effectTemplate.gameObject.activeSelf)
            {
                // 원본은 화면에 표시하지 않고 복제용 Template으로만 사용한다.
                _effectTemplate.gameObject.SetActive(false);
            }

            _previousPosition = transform.position;
            _distanceSinceSpawn = 0f;
            _hasPreviousPosition = true;
            _leadingEffect = null;
            SpawnEffect(_previousPosition);
        }

        private void LateUpdate()
        {
            Vector3 currentPosition = transform.position;

            if (!_hasPreviousPosition)
            {
                _previousPosition = currentPosition;
                _hasPreviousPosition = true;
                SpawnEffect(currentPosition);
                return;
            }

            SpawnAlongPath(
                _previousPosition,
                currentPosition);
            AlignLeadingEffect(currentPosition);
            _previousPosition = currentPosition;
        }

        private void SpawnAlongPath(
            Vector3 p_from,
            Vector3 p_to)
        {
            Vector3 horizontalDelta = p_to - p_from;
            horizontalDelta.y = 0f;

            float segmentDistance = horizontalDelta.magnitude;

            if (segmentDistance <= 0.0001f)
                return;

            float spawnDistance = Mathf.Max(
                0.01f,
                _spawnDistance);
            float remainingDistance = segmentDistance;
            float traveledDistance = 0f;

            while (_distanceSinceSpawn + remainingDistance >=
                   spawnDistance)
            {
                float distanceToSpawn =
                    spawnDistance - _distanceSinceSpawn;

                traveledDistance += distanceToSpawn;
                remainingDistance -= distanceToSpawn;

                float pathRatio = Mathf.Clamp01(
                    traveledDistance / segmentDistance);

                SpawnEffect(Vector3.Lerp(
                    p_from,
                    p_to,
                    pathRatio));

                _distanceSinceSpawn = 0f;
            }

            _distanceSinceSpawn += remainingDistance;
        }

        private void SpawnEffect(Vector3 p_pathPosition)
        {
            if (_effectTemplate == null ||
                !TryResolveGroundPoint(
                    p_pathPosition,
                    out Vector3 groundPoint))
            {
                return;
            }

            Quaternion rotation = ResolveGroundRotation();

            GameObject effect = Instantiate(
                _effectTemplate.gameObject,
                groundPoint,
                rotation);

            effect.name = $"{_effectTemplate.name} (Ground Wave)";
            effect.transform.localScale =
                _effectTemplate.lossyScale;
            effect.SetActive(true);

            RestartAsOneShot(effect);
            _leadingEffect = effect.transform;
            Destroy(
                effect,
                Mathf.Max(0.01f, _effectLifetime));
        }

        private void AlignLeadingEffect(Vector3 p_pathPosition)
        {
            if (_leadingEffect == null ||
                !TryResolveGroundPoint(
                    p_pathPosition,
                    out Vector3 groundPoint))
            {
                return;
            }

            // 이미 방출된 World Space Particle은 남기고 최신 Effect Root만 선두를 따라간다.
            _leadingEffect.SetPositionAndRotation(
                groundPoint,
                ResolveGroundRotation());
        }

        private Quaternion ResolveGroundRotation()
        {
            Vector3 horizontalForward = transform.forward;
            horizontalForward.y = 0f;

            if (horizontalForward.sqrMagnitude <= 0.0001f)
                horizontalForward = Vector3.forward;

            return Quaternion.LookRotation(
                horizontalForward.normalized,
                Vector3.up);
        }

        private bool TryResolveGroundPoint(
            Vector3 p_pathPosition,
            out Vector3 p_groundPoint)
        {
            float probeHeight = Mathf.Max(
                0f,
                _groundProbeHeight);
            float probeDepth = Mathf.Max(
                0.01f,
                _groundProbeDepth);

            Vector3 origin = p_pathPosition +
                             Vector3.up * probeHeight;

            if (Physics.Raycast(
                    origin,
                    Vector3.down,
                    out RaycastHit hit,
                    probeHeight + probeDepth,
                    _groundMask,
                    QueryTriggerInteraction.Ignore))
            {
                p_groundPoint = hit.point;
                return true;
            }

            p_groundPoint = default;
            return false;
        }

        private static void RestartAsOneShot(GameObject p_effect)
        {
            ParticleSystem[] particles =
                p_effect.GetComponentsInChildren<ParticleSystem>(true);

            for (int index = 0; index < particles.Length; index++)
            {
                ParticleSystem particle = particles[index];
                ParticleSystem.MainModule main = particle.main;

                main.loop = false;
                particle.Stop(
                    false,
                    ParticleSystemStopBehavior
                        .StopEmittingAndClear);
            }

            for (int index = 0; index < particles.Length; index++)
                particles[index].Play(false);
        }

        private void OnDisable()
        {
            AlignLeadingEffect(transform.position);
            _leadingEffect = null;
            _hasPreviousPosition = false;
            _distanceSinceSpawn = 0f;
        }

        private void OnValidate()
        {
            _spawnDistance = Mathf.Max(
                0.01f,
                _spawnDistance);
            _groundProbeHeight = Mathf.Max(
                0f,
                _groundProbeHeight);
            _groundProbeDepth = Mathf.Max(
                0.01f,
                _groundProbeDepth);
            _effectLifetime = Mathf.Max(
                0.01f,
                _effectLifetime);
        }
    }
}
