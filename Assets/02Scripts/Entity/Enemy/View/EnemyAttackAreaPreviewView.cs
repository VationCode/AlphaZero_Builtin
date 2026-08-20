using Alpha.Detection;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Alpha.Enemy.View
{
    // Enemy 공격 패턴의 거리와 실제 판정 영역을 Scene View에 표현한다.
    [DisallowMultipleComponent]
    public sealed class EnemyAttackAreaPreviewView : MonoBehaviour
    {
        private const int CircleSegments = 64;

        [SerializeField]
        private EnemyCombatModule _combatModule;

        [Tooltip("편집 모드에서 공격 방향의 기준으로 사용할 Transform")]
        [SerializeField]
        private Transform _origin;

        [Header("Preview")]
        [SerializeField]
        private bool _showAttackPreview = true;

        [SerializeField]
        private bool _showDistance = true;

        [SerializeField]
        private bool _showActualArea = true;

        [SerializeField]
        private bool _showPatternLabel = true;

        [Header("Type Colors")]
        [SerializeField]
        private Color _meleeColor = new(1f, 0.2f, 0.15f, 1f);

        [SerializeField]
        private Color _rangeColor = new(0f, 0.85f, 1f, 1f);

        [SerializeField]
        private Color _rushColor = new(1f, 0.75f, 0f, 1f);

        [Tooltip("Range Projectile의 Radial 피해 범위를 표시할 색상")]
        [SerializeField]
        private Color _radialDamageColor =
            new(1f, 0.25f, 0.1f, 0.2f);

        private void Reset()
        {
            _combatModule = ResolveCombatModule();
            _origin = ResolveOrigin(_combatModule);
        }

        private void OnValidate()
        {
            _combatModule ??= ResolveCombatModule();
            _origin ??= ResolveOrigin(_combatModule);
        }

        private void OnDrawGizmosSelected()
        {
            if (!_showAttackPreview)
                return;

            EnemyCombatModule combat = ResolveCombatModule();
            Transform origin = ResolveOrigin(combat);

            if (combat == null || origin == null)
                return;

            Color previousColor = Gizmos.color;

            for (int index = 0; index < combat.PatternCount; index++)
            {
                EnemyAttackPatternSetting pattern =
                    combat.GetPattern(index);

                if (pattern == null)
                    continue;

                bool isCurrentPattern =
                    Application.isPlaying &&
                    ReferenceEquals(combat.CurrentPattern, pattern);

                Color color = ResolvePatternColor(
                    pattern.AttackType,
                    index,
                    isCurrentPattern);

                if (_showDistance)
                    DrawAttackDistance(origin.position, pattern, color);

                if (_showActualArea)
                {
                    DrawTypeArea(
                        origin,
                        pattern,
                        color,
                        _radialDamageColor);
                }

#if UNITY_EDITOR
                if (_showPatternLabel)
                    DrawPatternLabel(origin, pattern, index, color);
#endif
            }

            Gizmos.color = previousColor;
        }

        private void DrawAttackDistance(
            Vector3 p_center,
            EnemyAttackPatternSetting p_pattern,
            Color p_color)
        {
            if (p_pattern.MinimumDistance > 0f)
            {
                Color minimumColor = p_color;
                minimumColor.a *= 0.55f;

                DrawHorizontalCircle(
                    p_center,
                    p_pattern.MinimumDistance,
                    minimumColor,
                    true);
            }

            DrawHorizontalCircle(
                p_center,
                p_pattern.MaximumDistance,
                p_color,
                false);
        }

        private static void DrawTypeArea(
            Transform p_origin,
            EnemyAttackPatternSetting p_pattern,
            Color p_color,
            Color p_radialDamageColor)
        {
            switch (p_pattern.AttackType)
            {
                case EEnemyAttackType.Melee:
                    DrawDetectionArea(
                        p_origin.position,
                        p_origin,
                        p_pattern.MeleeArea,
                        p_color);
                    break;

                case EEnemyAttackType.Range:
                    DrawRangePath(
                        p_origin,
                        p_pattern,
                        p_color,
                        p_radialDamageColor);
                    break;

                case EEnemyAttackType.Rush:
                    DrawRushPath(p_origin, p_pattern, p_color);
                    break;
            }
        }

        private static void DrawRangePath(
            Transform p_origin,
            EnemyAttackPatternSetting p_pattern,
            Color p_color,
            Color p_radialDamageColor)
        {
            Vector3 launchPosition =
                p_pattern.ProjectileSpawnPoint != null
                    ? p_pattern.ProjectileSpawnPoint.position
                    : p_origin.TransformPoint(
                        new Vector3(0f, 0.9f, 0.75f));

            Vector3 endPosition = launchPosition +
                                  p_origin.forward *
                                  p_pattern.MaximumDistance;

            var projectilePrefab =
                p_pattern.ProjectileLaunchSettings.Prefab;

            float collisionRadius = projectilePrefab != null
                ? Mathf.Max(0.01f, projectilePrefab.CollisionRadius)
                : 0.01f;

            Gizmos.color = p_color;
            Gizmos.DrawWireSphere(launchPosition, collisionRadius);
            DrawArrow(launchPosition, endPosition, p_color);
            Gizmos.DrawWireSphere(endPosition, collisionRadius);

            if (projectilePrefab != null &&
                projectilePrefab.ImpactSettings.IsRadial)
            {
                DrawRadialDamageArea(
                    endPosition,
                    projectilePrefab.ImpactSettings.DamageRadius,
                    p_radialDamageColor);
            }
        }

        private static void DrawRadialDamageArea(
            Vector3 p_center,
            float p_radius,
            Color p_color)
        {
            if (p_radius <= 0f)
                return;

            Gizmos.color = p_color;
            Gizmos.DrawSphere(p_center, p_radius);

            Color wireColor = p_color;
            wireColor.a = 1f;

            Gizmos.color = wireColor;
            Gizmos.DrawWireSphere(p_center, p_radius);
        }

        private static void DrawRushPath(
            Transform p_origin,
            EnemyAttackPatternSetting p_pattern,
            Color p_color)
        {
            Vector3 startPosition = p_origin.position;
            Vector3 endPosition = startPosition +
                                  p_origin.forward *
                                  p_pattern.RushDistance;

            DrawArrow(startPosition, endPosition, p_color);

            Color destinationColor = p_color;
            destinationColor.a *= 0.45f;

            DrawDetectionArea(
                startPosition,
                p_origin,
                p_pattern.RushArea,
                p_color);

            DrawDetectionArea(
                endPosition,
                p_origin,
                p_pattern.RushArea,
                destinationColor);
        }

        private static void DrawDetectionArea(
            Vector3 p_position,
            Transform p_origin,
            DetectionAreaSettings p_area,
            Color p_color)
        {
            if (p_area == null || !p_area.IsValid)
                return;

            DetectionAreaRequest request = new(
                p_position,
                p_origin.forward,
                p_origin.up,
                p_origin,
                p_area);

            DetectionAreaGizmoDrawer.Draw(request, p_color);
        }

        private Color ResolvePatternColor(
            EEnemyAttackType p_attackType,
            int p_patternIndex,
            bool p_isCurrentPattern)
        {
            Color color = p_attackType switch
            {
                EEnemyAttackType.Melee => _meleeColor,
                EEnemyAttackType.Range => _rangeColor,
                EEnemyAttackType.Rush => _rushColor,
                _ => Color.white
            };

            if (p_patternIndex > 0)
                color = Color.Lerp(color, Color.white, 0.25f);

            if (p_isCurrentPattern)
                color = Color.Lerp(color, Color.white, 0.45f);

            return color;
        }

        private EnemyCombatModule ResolveCombatModule()
        {
            if (_combatModule != null)
                return _combatModule;

            EnemyCore core = GetComponentInParent<EnemyCore>();

            if (core != null)
            {
                _combatModule = core.CombatModule ??
                                core.GetComponentInChildren<
                                    EnemyCombatModule>(true);
            }
            else
            {
                _combatModule =
                    GetComponentInChildren<EnemyCombatModule>(true) ??
                    GetComponentInParent<EnemyCombatModule>();
            }

            return _combatModule;
        }

        private Transform ResolveOrigin(EnemyCombatModule p_combat)
        {
            if (Application.isPlaying && p_combat?.Owner != null)
                return p_combat.Owner;

            if (_origin != null)
                return _origin;

            if (p_combat?.Owner != null)
                return p_combat.Owner;

            EnemyCore core = GetComponentInParent<EnemyCore>();
            return core != null ? core.transform : transform;
        }

        private static void DrawHorizontalCircle(
            Vector3 p_center,
            float p_radius,
            Color p_color,
            bool p_dashed)
        {
            if (p_radius <= 0f)
                return;

            Gizmos.color = p_color;
            Vector3 previousPoint =
                p_center + Vector3.right * p_radius;

            for (int index = 1; index <= CircleSegments; index++)
            {
                float angle = index / (float)CircleSegments *
                              Mathf.PI * 2f;

                Vector3 nextPoint = p_center +
                                    new Vector3(
                                        Mathf.Cos(angle),
                                        0f,
                                        Mathf.Sin(angle)) * p_radius;

                if (!p_dashed || index % 2 == 0)
                    Gizmos.DrawLine(previousPoint, nextPoint);

                previousPoint = nextPoint;
            }
        }

        private static void DrawArrow(
            Vector3 p_start,
            Vector3 p_end,
            Color p_color)
        {
            Vector3 direction = p_end - p_start;

            if (direction.sqrMagnitude <= 0.0001f)
                return;

            direction.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, direction);

            if (right.sqrMagnitude <= 0.0001f)
                right = Vector3.right;

            float headLength = Mathf.Min(
                0.5f,
                Vector3.Distance(p_start, p_end) * 0.2f);

            Gizmos.color = p_color;
            Gizmos.DrawLine(p_start, p_end);
            Gizmos.DrawLine(
                p_end,
                p_end - direction * headLength +
                right.normalized * headLength * 0.5f);
            Gizmos.DrawLine(
                p_end,
                p_end - direction * headLength -
                right.normalized * headLength * 0.5f);
        }

#if UNITY_EDITOR
        private static void DrawPatternLabel(
            Transform p_origin,
            EnemyAttackPatternSetting p_pattern,
            int p_patternIndex,
            Color p_color)
        {
            Vector3 labelPosition = p_origin.position +
                                    Vector3.up *
                                    (1.8f + p_patternIndex * 0.35f);

            Handles.color = p_color;
            Handles.Label(
                labelPosition,
                $"Pattern {p_patternIndex + 1}: " +
                $"{p_pattern.PatternName} [{p_pattern.AttackType}]\n" +
                $"Distance {p_pattern.MinimumDistance:0.##} - " +
                $"{p_pattern.MaximumDistance:0.##}");
        }
#endif
    }
}
