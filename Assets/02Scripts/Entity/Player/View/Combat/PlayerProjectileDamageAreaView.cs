using Alpha.Item.Weapon.Range;
using Alpha.Player.Combat;
using UnityEngine;
using UnityEngine.Rendering;

namespace Alpha.Player.View.Combat
{
    // Player가 조준 중인 Projectile의 실제 Radial 피해 반경만 바닥에 표현한다.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatModule))]
    public sealed class PlayerProjectileDamageAreaView : MonoBehaviour
    {
        [SerializeField]
        private CombatModule _combatModule;

        [Header("Impact Prediction")]
        [Tooltip("실제 Projectile 비행을 나눠 검사할 시간 간격입니다. 작을수록 충돌 위치가 정밀합니다.")]
        [SerializeField, Range(0.005f, 0.1f)]
        private float _predictionStep = 0.02f;

        [Header("Ground Projection")]
        [Tooltip("범위 표시를 배치할 바닥 Layer입니다. Enemy와 벽 전용 Layer는 제외합니다.")]
        [SerializeField]
        private LayerMask _groundMask = 1;

        [SerializeField, Min(0f)]
        private float _groundProbeHeight = 2f;

        [SerializeField, Min(0.01f)]
        private float _groundProbeDepth = 30f;

        [Tooltip("수직 벽에 범위가 표시되지 않도록 허용할 최소 바닥 Normal Y입니다.")]
        [SerializeField, Range(0f, 1f)]
        private float _minimumGroundNormalY = 0.5f;

        [Header("Area Visual")]
        [SerializeField]
        private Material _material;

        [SerializeField, Range(8, 128)]
        private int _segments = 64;

        [SerializeField, Min(0.001f)]
        private float _width = 0.06f;

        [SerializeField, Min(0f)]
        private float _surfaceOffset = 0.04f;

        [SerializeField]
        private Color _color = new(1f, 0.2f, 0.05f, 0.9f);

        private LineRenderer _areaRenderer;
        private Material _runtimeMaterial;

        private void Awake()
        {
            ResolveCombatModule();
            Hide();
        }

        private void LateUpdate()
        {
            RangeWeapon weapon =
                ResolveCombatModule()?.CurrentRangeWeapon;

            if (weapon == null ||
                !weapon.IsSecondaryActive ||
                !weapon.TryGetProjectileRadialDamageRadius(
                    out float damageRadius) ||
                !weapon.TryGetAttackPose(
                    out Vector3 origin,
                    out Vector3 direction) ||
                !weapon.TryPredictProjectileImpact(
                    origin,
                    direction,
                    _predictionStep,
                    out Alpha.Projectile.ProjectileImpactResult impact) ||
                !TryResolveGround(
                    impact.Point,
                    out RaycastHit groundHit))
            {
                Hide();
                return;
            }

            EnsureRenderer();
            DrawArea(
                groundHit.point,
                groundHit.normal,
                damageRadius);
        }

        private bool TryResolveGround(
            Vector3 p_targetPoint,
            out RaycastHit p_groundHit)
        {
            float probeHeight = Mathf.Max(
                0f,
                _groundProbeHeight);
            float probeDepth = Mathf.Max(
                0.01f,
                _groundProbeDepth);
            Vector3 probeOrigin =
                p_targetPoint + Vector3.up * probeHeight;

            if (!Physics.Raycast(
                    probeOrigin,
                    Vector3.down,
                    out p_groundHit,
                    probeHeight + probeDepth,
                    _groundMask,
                    QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            // 수직 투영 결과라도 벽에 가까운 표면이면 표시하지 않는다.
            return p_groundHit.normal.y >=
                   Mathf.Clamp01(_minimumGroundNormalY);
        }

        private void DrawArea(
            Vector3 p_groundPoint,
            Vector3 p_groundNormal,
            float p_radius)
        {
            Vector3 normal =
                p_groundNormal.sqrMagnitude > 0.0001f
                    ? p_groundNormal.normalized
                    : Vector3.up;
            Vector3 tangent = Vector3.Cross(
                normal,
                Vector3.forward);

            if (tangent.sqrMagnitude <= 0.0001f)
                tangent = Vector3.Cross(normal, Vector3.right);

            tangent.Normalize();
            Vector3 bitangent =
                Vector3.Cross(normal, tangent).normalized;
            Vector3 center =
                p_groundPoint + normal * _surfaceOffset;

            _areaRenderer.positionCount = _segments;

            for (int index = 0; index < _segments; index++)
            {
                float angle =
                    Mathf.PI * 2f * index / _segments;
                Vector3 point =
                    center +
                    tangent * Mathf.Cos(angle) * p_radius +
                    bitangent * Mathf.Sin(angle) * p_radius;

                _areaRenderer.SetPosition(index, point);
            }

            _areaRenderer.startWidth = _width;
            _areaRenderer.endWidth = _width;
            _areaRenderer.startColor = _color;
            _areaRenderer.endColor = _color;
            _areaRenderer.enabled = true;
        }

        private void EnsureRenderer()
        {
            if (_areaRenderer != null)
                return;

            GameObject rendererObject =
                new("ProjectileDamageAreaRenderer");
            rendererObject.transform.SetParent(transform, false);
            rendererObject.layer = gameObject.layer;

            _areaRenderer =
                rendererObject.AddComponent<LineRenderer>();
            _areaRenderer.useWorldSpace = true;
            _areaRenderer.loop = true;
            _areaRenderer.alignment = LineAlignment.View;
            _areaRenderer.textureMode = LineTextureMode.Stretch;
            _areaRenderer.numCornerVertices = 2;
            _areaRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _areaRenderer.receiveShadows = false;
            _areaRenderer.sharedMaterial =
                _material != null
                    ? _material
                    : CreateRuntimeMaterial();
            _areaRenderer.enabled = false;
        }

        private Material CreateRuntimeMaterial()
        {
            if (_runtimeMaterial != null)
                return _runtimeMaterial;

            Shader shader = Shader.Find("Sprites/Default");

            if (shader == null)
                shader = Shader.Find("Unlit/Color");

            if (shader == null)
                return null;

            _runtimeMaterial = new Material(shader)
            {
                name = "Projectile Damage Area (Runtime)",
                hideFlags = HideFlags.HideAndDontSave
            };

            return _runtimeMaterial;
        }

        private CombatModule ResolveCombatModule()
        {
            _combatModule ??= GetComponent<CombatModule>();
            return _combatModule;
        }

        private void Hide()
        {
            if (_areaRenderer != null)
                _areaRenderer.enabled = false;
        }

        private void OnDisable()
        {
            Hide();
        }

        private void OnDestroy()
        {
            if (_runtimeMaterial != null)
                Destroy(_runtimeMaterial);
        }

        private void OnValidate()
        {
            ResolveCombatModule();
            _predictionStep = Mathf.Clamp(
                _predictionStep,
                0.005f,
                0.1f);
            _groundProbeHeight = Mathf.Max(
                0f,
                _groundProbeHeight);
            _groundProbeDepth = Mathf.Max(
                0.01f,
                _groundProbeDepth);
            _minimumGroundNormalY = Mathf.Clamp01(
                _minimumGroundNormalY);
            _segments = Mathf.Clamp(_segments, 8, 128);
            _width = Mathf.Max(0.001f, _width);
            _surfaceOffset = Mathf.Max(0f, _surfaceOffset);
        }
    }
}
