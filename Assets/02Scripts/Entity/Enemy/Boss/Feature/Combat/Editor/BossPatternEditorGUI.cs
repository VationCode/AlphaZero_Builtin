using System;
using Alpha.Detection;
using UnityEditor;
using UnityEngine;

namespace Alpha.Boss.Editor
{
    // 중앙 목록과 거리별 목록에서 같은 실행 조건과 영역 미리보기를 사용한다.
    internal static class BossPatternEditorGUI
    {
        internal static void DrawExecutionButtons(BossCore p_boss, int p_count,
            Func<int, BossPatternData> p_getPattern, UnityEngine.Object p_owner)
        {
            if (!Application.isPlaying || p_boss == null)
                return;
            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(p_boss.CombatContext.State != EBossCombatState.Idle ||
                       p_boss.EncounterContext.CurrentState != EBossEncounterState.Combat || p_boss.HealthContext.IsDead))
            {
                for (int i = 0; i < p_count; i++)
                {
                    BossPatternData pattern = p_getPattern(i);
                    if (pattern == null || (pattern.AttackType != EBossAttackType.Melee && pattern.AttackType != EBossAttackType.Range &&
                        pattern.AttackType != EBossAttackType.Arena && pattern.AttackType != EBossAttackType.Area &&
                        pattern.AttackType != EBossAttackType.Rush))
                        continue;
                    if (GUILayout.Button($"실행: {pattern.PatternName}") && !p_boss.TryStartAttack(pattern))
                        Debug.LogWarning("공격을 시작하지 못했습니다. 공격 설정, 타겟, Rigidbody(Is Kinematic 해제), Prefab과 애니메이션 키를 확인하세요.", p_owner);
                }
            }
            if (GUILayout.Button("공격 취소"))
                p_boss.CancelAttack();
        }

        internal static void DrawPatternPreviews(BossCore p_boss, Transform p_fallback,
            int p_count, Func<int, BossPatternData> p_getPattern)
        {
            Color color = Gizmos.color;
            Matrix4x4 matrix = Gizmos.matrix;
            try
            {
                Gizmos.color = Color.cyan;
                for (int i = 0; i < p_count; i++)
                {
                    BossPatternData pattern = p_getPattern(i);
                    if (pattern == null)
                        continue;
                    // 실행 거리 표시는 공격 영역 토글과 독립적이다.
                    DrawSelectionDistancePreview(p_boss, p_fallback, pattern, i);
                    if (!pattern.ShowAttackRange)
                        continue;
                    if (pattern.AttackType == EBossAttackType.Range)
                    {
                        DrawRangePreview(p_boss, pattern);
                        continue;
                    }
                    if (pattern.AttackType == EBossAttackType.Area)
                    {
                        DrawAreaPreview(p_boss, p_fallback, pattern, i);
                        continue;
                    }
                    if (pattern != null && (pattern.AttackType == EBossAttackType.Melee ||
                        pattern.AttackType == EBossAttackType.Rush))
                    {
                        DrawDamagePreview(p_boss, p_fallback, pattern, i);
                        continue;
                    }
                    if (pattern?.AttackType != EBossAttackType.Arena || pattern.ArenaAttack == null)
                        continue;
                    Gizmos.matrix = Matrix4x4.identity;
                    for (int groupIndex = 0; groupIndex < pattern.ArenaAttack.SpawnGroupCount; groupIndex++)
                    {
                        BossArenaSpawnGroup group = pattern.ArenaAttack.GetSpawnGroup(groupIndex);
                        if (group == null)
                            continue;
                        for (int pointIndex = 0; pointIndex < group.SpawnPointCount; pointIndex++)
                        {
                            Transform point = group.GetSpawnPoint(pointIndex);
                            if (point == null)
                                continue;
                            Gizmos.DrawWireSphere(point.position, 0.5f);
                            Gizmos.DrawLine(point.position,
                                point.position + point.forward * pattern.ArenaAttack.MaximumDistance);
                            Handles.Label(point.position, $"{pattern.PatternName} · {group.SpawnTimeSeconds:0.##}s");
                        }
                    }
                }
            }
            finally
            {
                Gizmos.color = color;
                Gizmos.matrix = matrix;
            }
        }

        private static void DrawSelectionDistancePreview(BossCore p_boss, Transform p_fallback,
            BossPatternData p_pattern, int p_index)
        {
            if ((!p_pattern.ShowMinimumDistance && !p_pattern.ShowMaximumDistance) || p_pattern.Selection == null)
                return;
            Transform owner = p_boss != null ? p_boss.transform : p_fallback;
            if (owner == null)
                return;
            // 실제 패턴 선택과 동일한 수평 거리 기준이다. 모델 스케일의 영향을 받지 않는다.
            Vector3 center = p_boss != null && p_boss.Rigidbody != null && Application.isPlaying
                ? p_boss.Rigidbody.position : owner.position;
            if (float.IsNaN(center.sqrMagnitude) || float.IsInfinity(center.sqrMagnitude))
                return;
            center += Vector3.up * 0.03f;
            Vector3 labelDirection = Quaternion.Euler(0f, p_index * 137.5f, 0f) * Vector3.forward;
            var depth = Handles.zTest;
            using (new Handles.DrawingScope(Matrix4x4.identity))
            {
                try
                {
                    Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
                    if (p_pattern.ShowMinimumDistance)
                        DrawDistanceRing(center, p_pattern.Selection.MinimumDistance, -labelDirection,
                            Color.yellow, p_pattern.PatternName, "최소 실행 거리");
                    if (p_pattern.ShowMaximumDistance)
                        DrawDistanceRing(center, p_pattern.Selection.MaximumDistance, labelDirection,
                            Color.cyan, p_pattern.PatternName, "최대 실행 거리");
                }
                finally { Handles.zTest = depth; }
            }
        }

        private static void DrawDistanceRing(Vector3 p_center, float p_distance, Vector3 p_labelDirection,
            Color p_color, string p_patternName, string p_label)
        {
            if (float.IsNaN(p_distance) || float.IsInfinity(p_distance) || p_distance < 0f)
                return;
            Handles.color = p_color;
            if (p_distance > 0f)
                Handles.DrawWireDisc(p_center, Vector3.up, p_distance, 2f);
            Handles.Label(p_center + p_labelDirection * p_distance,
                $"{p_patternName} | {p_label} {p_distance:0.##}m");
        }

        // 투사체 충돌 영역은 Prefab이 소유한다. 여기서는 발사점별 진행 방향과 최대 이동 거리만 표시한다.
        private static void DrawRangePreview(BossCore p_boss, BossPatternData p_pattern)
        {
            BossRangeAttackSettings settings = p_pattern.RangeAttack;
            if (settings == null || float.IsNaN(settings.MaximumDistance) ||
                float.IsInfinity(settings.MaximumDistance) || settings.MaximumDistance <= 0f)
                return;
            Transform target = p_boss != null ? p_boss.Target : null;
            Collider targetCollider = target != null
                ? target.GetComponent<Collider>() ?? target.GetComponentInChildren<Collider>() : null;
            Vector3 targetPoint = targetCollider != null && targetCollider.enabled
                ? targetCollider.bounds.center : target != null ? target.position : Vector3.zero;
            using (new Handles.DrawingScope(Color.cyan, Matrix4x4.identity))
            {
                for (int groupIndex = 0; groupIndex < settings.FireGroupCount; groupIndex++)
                {
                    BossRangeFireGroup group = settings.GetFireGroup(groupIndex);
                    if (group == null)
                        continue;
                    for (int i = 0; i < group.SpawnPointCount; i++)
                    {
                        Transform spawn = group.GetSpawnPoint(i);
                        if (spawn == null)
                            continue;
                        bool hasTarget = settings.DirectionType == EBossRangeDirectionType.Target && target != null;
                        Vector3 direction = hasTarget ? targetPoint - spawn.position : spawn.forward;
                        if (settings.AttackMode == EBossRangeAttackMode.GroundWave)
                            direction.y = 0f;
                        if (direction.sqrMagnitude <= 0.0001f || float.IsNaN(direction.sqrMagnitude) ||
                            float.IsInfinity(direction.sqrMagnitude))
                            continue;
                        Vector3 end = spawn.position + direction.normalized * settings.MaximumDistance;
                        Handles.DrawLine(spawn.position, end);
                        string basis = settings.DirectionType == EBossRangeDirectionType.Target && !hasTarget
                            ? " · 타겟 없음, 발사점 정면 기준" : string.Empty;
                        Handles.Label(end, $"{p_pattern.PatternName} | 이동 거리 {settings.MaximumDistance:0.##}m{basis}");
                    }
                }
            }
        }

        private static void DrawDamagePreview(BossCore p_boss, Transform p_fallback, BossPatternData p_pattern, int p_index)
        {
            Transform owner = p_boss != null ? p_boss.transform : p_fallback;
            DetectionAreaSettings area = p_pattern.Damage?.Area;
            if (owner == null || area == null || !area.IsValid || !p_pattern.Damage.Enabled)
                return;
            Vector3 forward = owner.forward;
            if (p_boss != null && p_boss.CombatContext.CurrentPattern == p_pattern && p_boss.CombatContext.Rush.Started)
                forward = p_boss.CombatContext.Rush.Direction;
            forward.y = 0f;
            DetectionAreaRequest request = new(owner.position, forward, Vector3.up, owner, area);
            Color color = Color.HSVToRGB(Mathf.Repeat(p_index * 0.618034f, 1f), 0.7f, 1f);
            using (new Handles.DrawingScope(color, Matrix4x4.TRS(request.AreaOrigin, request.Rotation, Vector3.one)))
            {
                if (area.Shape == EDetectionAreaShape.ForwardBox)
                    Handles.DrawWireCube(Vector3.forward * (area.Length * 0.5f), new Vector3(area.Width, area.Height, area.Length));
                else
                {
                    float angle = area.Shape == EDetectionAreaShape.Radial ? 360f : area.Angle;
                    Vector3 left = Quaternion.Euler(0f, -angle * 0.5f, 0f) * Vector3.forward;
                    Vector3 right = Quaternion.Euler(0f, angle * 0.5f, 0f) * Vector3.forward;
                    Vector3 bottom = Vector3.down * (area.Height * 0.5f);
                    Vector3 top = -bottom;
                    Handles.DrawWireArc(bottom, Vector3.up, left, angle, area.Radius);
                    Handles.DrawWireArc(top, Vector3.up, left, angle, area.Radius);
                    Handles.DrawLine(bottom + left * area.Radius, top + left * area.Radius);
                    Handles.DrawLine(bottom + right * area.Radius, top + right * area.Radius);
                    if (angle < 360f)
                    {
                        Handles.DrawLine(bottom, bottom + left * area.Radius);
                        Handles.DrawLine(bottom, bottom + right * area.Radius);
                        Handles.DrawLine(top, top + left * area.Radius);
                        Handles.DrawLine(top, top + right * area.Radius);
                    }
                }
                Handles.Label(Vector3.up * (area.Height * 0.5f), $"{p_pattern.PatternName} · Damage");
            }
        }

        private static void DrawAreaPreview(BossCore p_boss, Transform p_fallback, BossPatternData p_pattern,
            int p_colorIndex)
        {
            if (!TryGetAreaPreviewCenter(p_boss, p_fallback, p_pattern.AreaAttack,
                    out Vector3 center, out bool hasTarget, out bool grounded))
                return;
            float radius = p_pattern.AreaAttack.SpawnRadius;
            Color color = Color.HSVToRGB(Mathf.Repeat(0.12f + p_colorIndex * 0.618034f, 1f), 0.7f, 1f);
            var depth = Handles.zTest;
            using (new Handles.DrawingScope(color, Matrix4x4.identity))
            {
                try
                {
                    Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
                    Vector3 drawCenter = center + Vector3.up * 0.03f;
                    Handles.color = new Color(color.r, color.g, color.b, 0.05f);
                    Handles.DrawSolidDisc(drawCenter, Vector3.up, radius);
                    Handles.color = color;
                    Handles.DrawWireDisc(drawCenter, Vector3.up, radius, 2f);
                    string basis = hasTarget ? "타겟 기준" : "타겟 없음 · 보스/선택 객체 기준";
                    string ground = grounded ? "" : " · 지면 미검출";
                    Handles.Label(drawCenter + Vector3.forward * radius,
                        $"{p_pattern.PatternName} | Spawn Radius {radius:0.##}m\n{basis}{ground}");
                }
                finally { Handles.zTest = depth; }
            }
        }

        internal static bool TryGetAreaPreviewCenter(BossCore p_boss, Transform p_fallback,
            BossAreaAttackSettings p_settings, out Vector3 p_center, out bool p_hasTarget, out bool p_grounded)
        {
            p_center = default;
            p_grounded = false;
            Transform target = p_boss != null ? p_boss.Target : null;
            p_hasTarget = target != null;
            Transform owner = p_boss != null ? p_boss.transform : p_fallback;
            Transform basis = target != null ? target : owner;
            if (basis == null || p_settings == null || float.IsNaN(p_settings.SpawnRadius) ||
                float.IsInfinity(p_settings.SpawnRadius) || p_settings.SpawnRadius < 0f)
                return false;
            p_center = basis.position;
            if (float.IsNaN(p_center.sqrMagnitude) || float.IsInfinity(p_center.sqrMagnitude))
                return false;
            if (BossAreaAttackModule.TryFindGround(owner, target, p_settings, p_center, out Vector3 ground))
            {
                p_center = ground;
                p_grounded = true;
            }
            return true;
        }
    }
}
