using Alpha.Detection;
using Alpha.Projectile;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Alpha.Enemy.View
{
    // Enemy 공격 패턴의 거리와 실제 판정 영역을 Scene View에 표현한다.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DetectionAreaGizmoView))]
    public sealed class EnemyAttackAreaPreviewView : MonoBehaviour
    {
        private const int CircleSegments = 64;

        [SerializeField]
        private EnemyCombatModule _combatModule;

        [SerializeField]
        private DetectionAreaGizmoView _areaGizmoView;

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

        // Pattern Settings Inspector가 관리하는 공격 원본별 Preview 상태다.
        [SerializeField, HideInInspector]
        private bool[] _attackPatternVisibility =
            System.Array.Empty<bool>();

        // Combat Inspector가 관리하는 거리 규칙별 Preview 상태다.
        [FormerlySerializedAs("_patternVisibility")]
        [SerializeField, HideInInspector]
        private bool[] _distancePatternVisibility =
            System.Array.Empty<bool>();

        [Header("Type Colors")]
        [SerializeField]
        private Color _meleeColor = new(1f, 0.2f, 0.15f, 1f);

        [SerializeField]
        private Color _rangeColor = new(0f, 0.85f, 1f, 1f);

        [SerializeField]
        private Color _rushColor = new(1f, 0.75f, 0f, 1f);

        [SerializeField]
        private Color _areaAttackColor =
            new(0.65f, 0.25f, 1f, 1f);

        [SerializeField]
        private Color _arenaAttackColor =
            new(1f, 0.15f, 0.65f, 1f);

        [Tooltip("Range Projectile의 Radial 피해 범위를 표시할 색상")]
        [SerializeField]
        private Color _radialDamageColor =
            new(1f, 0.25f, 0.1f, 0.2f);

        private void Reset()
        {
            _combatModule = ResolveCombatModule();
            _areaGizmoView = GetComponent<DetectionAreaGizmoView>();
            _origin = ResolveOrigin(_combatModule);
        }

        private void OnValidate()
        {
            _combatModule ??= ResolveCombatModule();
            _areaGizmoView ??= GetComponent<DetectionAreaGizmoView>();
            _origin ??= ResolveOrigin(_combatModule);
        }

        private void OnDrawGizmosSelected()
        {
            _areaGizmoView ??= GetComponent<DetectionAreaGizmoView>();

            if (!_showAttackPreview || _areaGizmoView == null)
                return;

            EnemyCombatModule combat = ResolveCombatModule();
            Transform origin = ResolveOrigin(combat);

            if (combat == null || origin == null)
                return;

            Color previousColor = Gizmos.color;

            DrawAttackPatternPreviews(combat, origin);
            DrawDistancePatternPreviews(combat, origin);

            Gizmos.color = previousColor;
        }

        // 거리 규칙에 연결하기 전에도 각 Pattern Setting의 실제 범위를 표시한다.
        private void DrawAttackPatternPreviews(
            EnemyCombatModule p_combat,
            Transform p_origin)
        {
            for (int index = 0;
                 index < p_combat.PatternCount;
                 index++)
            {
                if (!IsAttackPatternVisible(index))
                    continue;

                EnemyAttackPatternSetting pattern =
                    p_combat.GetPattern(index);

                if (pattern == null)
                    continue;

                bool isCurrentPattern =
                    Application.isPlaying &&
                    ReferenceEquals(p_combat.CurrentPattern, pattern);

                Color color = ResolvePatternColor(
                    pattern.AttackType,
                    index,
                    isCurrentPattern);

                if (_showActualArea)
                {
                    DrawTypeArea(
                        p_origin,
                        pattern,
                        color,
                        _radialDamageColor);
                }

#if UNITY_EDITOR
                if (_showPatternLabel)
                {
                    DrawAttackPatternLabel(
                        p_origin,
                        pattern,
                        index,
                        color);
                }
#endif
            }
        }

        // CombatModule이 소유한 거리별 선택 범위만 별도로 표시한다.
        private void DrawDistancePatternPreviews(
            EnemyCombatModule p_combat,
            Transform p_origin)
        {

            for (int index = 0;
                 index < p_combat.DistancePatternCount;
                 index++)
            {
                if (!IsDistancePatternVisible(index))
                    continue;

                EnemyDistancePatternSetting distancePattern =
                    p_combat.GetDistancePattern(index);
                EnemyAttackPatternSetting pattern =
                    p_combat.GetPattern(
                        distancePattern?.PatternIndex ?? -1);

                if (distancePattern == null || pattern == null)
                    continue;

                Color color = ResolvePatternColor(
                    pattern.AttackType,
                    index,
                    false);

                if (_showDistance)
                {
                    DrawAttackDistance(
                        p_origin.position,
                        distancePattern,
                        color);
                }

#if UNITY_EDITOR
                if (_showPatternLabel)
                {
                    DrawDistancePatternLabel(
                        p_origin,
                        distancePattern,
                        pattern,
                        index,
                        color);
                }
#endif
            }
        }

        // 아직 표시 설정이 생성되지 않은 Pattern은 기본적으로 표시한다.
        public bool IsAttackPatternVisible(int p_patternIndex)
        {
            if (p_patternIndex < 0)
                return false;

            return _attackPatternVisibility == null ||
                   p_patternIndex >= _attackPatternVisibility.Length ||
                   _attackPatternVisibility[p_patternIndex];
        }

        public bool IsDistancePatternVisible(int p_patternIndex)
        {
            if (p_patternIndex < 0)
                return false;

            return _distancePatternVisibility == null ||
                   p_patternIndex >= _distancePatternVisibility.Length ||
                   _distancePatternVisibility[p_patternIndex];
        }

        private void DrawAttackDistance(
            Vector3 p_center,
            EnemyDistancePatternSetting p_distancePattern,
            Color p_color)
        {
            if (p_distancePattern.MinimumDistance > 0f)
            {
                Color minimumColor = p_color;
                minimumColor.a *= 0.55f;

                DrawHorizontalCircle(
                    p_center,
                    p_distancePattern.MinimumDistance,
                    minimumColor,
                    true);
            }

            DrawHorizontalCircle(
                p_center,
                p_distancePattern.MaximumDistance,
                p_color,
                false);
        }

        private void DrawTypeArea(
            Transform p_origin,
            EnemyAttackPatternSetting p_pattern,
            Color p_color,
            Color p_radialDamageColor)
        {
            bool usesColliderTiming = DrawTimingColliders(
                p_pattern,
                p_color);

            switch (p_pattern.AttackType)
            {
                case EEnemyAttackType.Melee:
                    if (!usesColliderTiming)
                    {
                        DrawDetectionArea(
                            p_origin.position,
                            p_origin,
                            p_pattern.MeleeArea,
                            p_color);
                    }
                    break;

                case EEnemyAttackType.Range:
                    DrawRangePath(
                        p_origin,
                        p_pattern,
                        p_color,
                        p_radialDamageColor);
                    break;

                case EEnemyAttackType.Rush:
                    DrawRushPath(
                        p_origin,
                        p_pattern,
                        p_color,
                        !usesColliderTiming);
                    break;

                case EEnemyAttackType.Area:
                    if (!usesColliderTiming)
                    {
                        DrawDetectionArea(
                            p_origin.position,
                            p_origin,
                            p_pattern.AreaAttackArea,
                            p_color);
                    }
                    break;

                case EEnemyAttackType.Arena:
                    if (!usesColliderTiming)
                    {
                        DrawDetectionArea(
                            p_origin.position,
                            p_origin,
                            p_pattern.ArenaAttackArea,
                            p_color);
                    }
                    break;
            }
        }

        // Collider 타이밍을 사용하면 실제 참조된 Collider와 활성 구간을 표시한다.
        private static bool DrawTimingColliders(
            EnemyAttackPatternSetting p_pattern,
            Color p_color)
        {
            bool hasColliderTiming = false;
            Matrix4x4 previousMatrix = Gizmos.matrix;

            for (int index = 0;
                 index < p_pattern.AttackTimingCount;
                 index++)
            {
                EnemyAttackTimingSetting timing =
                    p_pattern.GetAttackTiming(index);

                if (timing == null ||
                    timing.EventType !=
                        EEnemyAttackTimingType.Collider ||
                    !timing.IsExecutable(p_pattern.AttackType))
                {
                    continue;
                }

                Collider attackCollider = timing.AttackCollider;
                hasColliderTiming = true;
                Gizmos.color = p_color;
                Gizmos.matrix =
                    attackCollider.transform.localToWorldMatrix;

                switch (attackCollider)
                {
                    case BoxCollider boxCollider:
                        Gizmos.DrawWireCube(
                            boxCollider.center,
                            boxCollider.size);
                        break;

                    case SphereCollider sphereCollider:
                        Gizmos.DrawWireSphere(
                            sphereCollider.center,
                            sphereCollider.radius);
                        break;

                    case CapsuleCollider capsuleCollider:
                        Vector3 capsuleSize = Vector3.one *
                                              capsuleCollider.radius * 2f;
                        capsuleSize[capsuleCollider.direction] =
                            capsuleCollider.height;
                        Gizmos.DrawWireCube(
                            capsuleCollider.center,
                            capsuleSize);
                        break;

                    case MeshCollider meshCollider
                        when meshCollider.sharedMesh != null:
                        Gizmos.DrawWireMesh(meshCollider.sharedMesh);
                        break;
                }

#if UNITY_EDITOR
                Handles.color = p_color;
                Handles.Label(
                    attackCollider.transform.position,
                    $"Collider {timing.StartTimeSeconds:0.00}s" +
                    $" - {timing.EndTimeSeconds:0.00}s");
#endif
            }

            Gizmos.matrix = previousMatrix;
            return hasColliderTiming;
        }

        private static void DrawRangePath(
            Transform p_origin,
            EnemyAttackPatternSetting p_pattern,
            Color p_color,
            Color p_radialDamageColor)
        {
            var projectilePrefab =
                p_pattern.ProjectilePrefab;

            float collisionRadius = projectilePrefab != null
                ? Mathf.Max(
                    0.01f,
                    projectilePrefab.CollisionPreviewRadius)
                : 0.01f;

            bool hasConfiguredFirePosition = false;

            for (int index = 0;
                 index < p_pattern.ProjectileSpawnPointSlotCount;
                 index++)
            {
                Transform firePosition =
                    p_pattern.GetProjectileSpawnPoint(index);

                if (firePosition == null)
                    continue;

                hasConfiguredFirePosition = true;

                Vector3 direction =
                    p_pattern.RangeDirectionType ==
                    EEnemyRangeDirectionType.FirePositionForward
                        ? firePosition.forward
                        : p_origin.forward;

                DrawSingleRangePath(
                    firePosition.position,
                    direction,
                    p_pattern.ProjectileMaximumDistance,
                    collisionRadius,
                    projectilePrefab,
                    p_color,
                    p_radialDamageColor);
            }

            if (hasConfiguredFirePosition)
                return;

            Vector3 fallbackPosition = p_origin.TransformPoint(
                new Vector3(0f, 0.9f, 0.75f));

            DrawSingleRangePath(
                fallbackPosition,
                p_origin.forward,
                p_pattern.ProjectileMaximumDistance,
                collisionRadius,
                projectilePrefab,
                p_color,
                p_radialDamageColor);
        }

        // 하나의 FirePos가 생성할 Projectile 경로와 충돌 크기를 표시한다.
        private static void DrawSingleRangePath(
            Vector3 p_launchPosition,
            Vector3 p_direction,
            float p_maximumDistance,
            float p_collisionRadius,
            Alpha.Projectile.Projectile p_projectilePrefab,
            Color p_color,
            Color p_radialDamageColor)
        {
            if (p_direction.sqrMagnitude <= 0.0001f)
                return;

            Vector3 endPosition = p_launchPosition +
                                  p_direction.normalized *
                                  p_maximumDistance;

            Gizmos.color = p_color;
            Gizmos.DrawWireSphere(
                p_launchPosition,
                p_collisionRadius);
            DrawArrow(p_launchPosition, endPosition, p_color);
            Gizmos.DrawWireSphere(endPosition, p_collisionRadius);

            if (p_projectilePrefab != null &&
                p_projectilePrefab.HasDamageArea)
            {
                DrawRadialDamageArea(
                    endPosition,
                    p_projectilePrefab.DamageAreaPreviewRadius,
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

        private void DrawRushPath(
            Transform p_origin,
            EnemyAttackPatternSetting p_pattern,
            Color p_color,
            bool p_drawDamageArea)
        {
            Vector3 startPosition = p_origin.position;
            Vector3 endPosition = startPosition +
                                  p_origin.forward *
                                  p_pattern.RushDistance;

            DrawArrow(startPosition, endPosition, p_color);

            Color destinationColor = p_color;
            destinationColor.a *= 0.45f;

            if (!p_drawDamageArea)
                return;

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

        private void DrawDetectionArea(
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

            _areaGizmoView.Draw(request, p_color);
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
                EEnemyAttackType.Area => _areaAttackColor,
                EEnemyAttackType.Arena => _arenaAttackColor,
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
        private static void DrawAttackPatternLabel(
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
                $"{p_pattern.PatternName} [{p_pattern.AttackType}]");
        }

        private static void DrawDistancePatternLabel(
            Transform p_origin,
            EnemyDistancePatternSetting p_distancePattern,
            EnemyAttackPatternSetting p_pattern,
            int p_patternIndex,
            Color p_color)
        {
            Vector3 labelPosition = p_origin.position +
                                    Vector3.up *
                                    (3.2f + p_patternIndex * 0.35f);

            Handles.color = p_color;
            Handles.Label(
                labelPosition,
                $"Distance {p_patternIndex + 1}: " +
                $"{p_distancePattern.RangeName} → " +
                $"{p_pattern.PatternName} [{p_pattern.AttackType}]\n" +
                $"Range {p_distancePattern.MinimumDistance:0.##} - " +
                $"{p_distancePattern.MaximumDistance:0.##}");
        }
#endif
    }
}
