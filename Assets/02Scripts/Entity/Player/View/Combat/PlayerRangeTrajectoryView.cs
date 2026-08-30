using Alpha.Item.Weapon.Range;
using Alpha.Player.Combat;
using UnityEngine;
using UnityEngine.Rendering;

namespace Alpha.Player.View.Combat
{
    // Player가 조준 중인 Projectile의 포물선과 착탄 범위를 게임 화면에 표현한다.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatModule))]
    public sealed class PlayerRangeTrajectoryView : MonoBehaviour
    {
        [SerializeField]
        private CombatModule _combatModule;

        [Header("Trajectory")]
        [SerializeField]
        private Material _material;

        [SerializeField, Min(0.01f)]
        private float _simulationStep = 0.05f;

        [SerializeField, Range(2, 512)]
        private int _maxPoints = 96;

        [SerializeField, Min(0.001f)]
        private float _trajectoryWidth = 0.04f;

        [SerializeField]
        private Color _trajectoryColor =
            new(1f, 0.65f, 0.1f, 0.9f);

        [Header("Impact")]
        [SerializeField, Range(8, 128)]
        private int _impactSegments = 64;

        [SerializeField, Min(0.001f)]
        private float _impactWidth = 0.06f;

        [SerializeField, Min(0f)]
        private float _surfaceOffset = 0.03f;

        [SerializeField]
        private Color _impactColor =
            new(1f, 0.2f, 0.05f, 0.9f);

        private LineRenderer _trajectoryRenderer;
        private LineRenderer _impactRenderer;
        private Vector3[] _trajectoryPoints;

        public bool Bind(CombatModule p_combatModule)
        {
            if (p_combatModule == null)
                return false;

            _combatModule = p_combatModule;
            return true;
        }

        public void Unbind()
        {
            _combatModule = null;
            Hide();
        }

        private void Awake()
        {
            _combatModule ??= GetComponent<CombatModule>();
            Hide();
        }

        private void LateUpdate()
        {
            RangeWeapon weapon = _combatModule?.CurrentRangeWeapon;

            if (weapon == null ||
                weapon.AttackType != ERangeAttackType.Projectile ||
                !weapon.IsSecondaryActive ||
                !_combatModule.TryGetRangeAttackPose(
                    out Vector3 origin,
                    out Vector3 direction))
            {
                Hide();
                return;
            }

            EnsureRenderers();
            Vector3[] points = ResolveTrajectoryBuffer();

            if (!weapon.TryPredictProjectileTrajectory(
                    origin,
                    direction,
                    _simulationStep,
                    points,
                    out ProjectileTrajectoryResult result))
            {
                Hide();
                return;
            }

            DrawTrajectory(points, result.PointCount);

            if (result.HasImpact &&
                weapon.TryGetProjectileRadialDamageRadius(
                    out float damageRadius))
            {
                DrawImpactRadius(
                    result.ImpactPoint,
                    result.ImpactNormal,
                    damageRadius);
            }
            else
            {
                _impactRenderer.enabled = false;
            }
        }

        private void DrawTrajectory(Vector3[] p_points, int p_pointCount)
        {
            _trajectoryRenderer.positionCount = p_pointCount;

            for (int index = 0; index < p_pointCount; index++)
                _trajectoryRenderer.SetPosition(index, p_points[index]);

            _trajectoryRenderer.enabled = p_pointCount > 1;
        }

        private void DrawImpactRadius(
            Vector3 p_impactPoint,
            Vector3 p_impactNormal,
            float p_radius)
        {
            Vector3 normal = p_impactNormal.sqrMagnitude > 0.0001f
                ? p_impactNormal.normalized
                : Vector3.up;
            Vector3 tangent = Vector3.Cross(normal, Vector3.up);

            if (tangent.sqrMagnitude <= 0.0001f)
                tangent = Vector3.Cross(normal, Vector3.right);

            tangent.Normalize();
            Vector3 bitangent = Vector3.Cross(normal, tangent);
            Vector3 center = p_impactPoint + normal * _surfaceOffset;
            _impactRenderer.positionCount = _impactSegments;

            for (int index = 0; index < _impactSegments; index++)
            {
                float angle = Mathf.PI * 2f * index / _impactSegments;
                Vector3 point =
                    center +
                    tangent * Mathf.Cos(angle) * p_radius +
                    bitangent * Mathf.Sin(angle) * p_radius;

                _impactRenderer.SetPosition(index, point);
            }

            _impactRenderer.enabled = true;
        }

        private void EnsureRenderers()
        {
            _trajectoryRenderer ??= CreateRenderer(
                "ProjectileTrajectoryRenderer",
                false,
                _trajectoryWidth,
                _trajectoryColor);

            _impactRenderer ??= CreateRenderer(
                "ProjectileImpactRadiusRenderer",
                true,
                _impactWidth,
                _impactColor);
        }

        private LineRenderer CreateRenderer(
            string p_name,
            bool p_loop,
            float p_width,
            Color p_color)
        {
            GameObject rendererObject = new(p_name);
            rendererObject.transform.SetParent(transform, false);
            rendererObject.layer = gameObject.layer;

            LineRenderer renderer =
                rendererObject.AddComponent<LineRenderer>();
            renderer.useWorldSpace = true;
            renderer.loop = p_loop;
            renderer.startWidth = p_width;
            renderer.endWidth = p_width;
            renderer.startColor = p_color;
            renderer.endColor = p_color;
            renderer.numCapVertices = 4;
            renderer.numCornerVertices = 2;
            renderer.alignment = LineAlignment.View;
            renderer.textureMode = LineTextureMode.Stretch;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sharedMaterial = _material;
            renderer.enabled = false;
            return renderer;
        }

        private Vector3[] ResolveTrajectoryBuffer()
        {
            if (_trajectoryPoints == null ||
                _trajectoryPoints.Length != _maxPoints)
            {
                _trajectoryPoints = new Vector3[_maxPoints];
            }

            return _trajectoryPoints;
        }

        private void Hide()
        {
            if (_trajectoryRenderer != null)
                _trajectoryRenderer.enabled = false;

            if (_impactRenderer != null)
                _impactRenderer.enabled = false;
        }

        private void OnDisable()
        {
            Hide();
        }

        private void OnValidate()
        {
            _combatModule ??= GetComponent<CombatModule>();
            _simulationStep = Mathf.Max(0.01f, _simulationStep);
            _maxPoints = Mathf.Clamp(_maxPoints, 2, 512);
            _trajectoryWidth = Mathf.Max(0.001f, _trajectoryWidth);
            _impactSegments = Mathf.Clamp(_impactSegments, 8, 128);
            _impactWidth = Mathf.Max(0.001f, _impactWidth);
            _surfaceOffset = Mathf.Max(0f, _surfaceOffset);
        }
    }
}
