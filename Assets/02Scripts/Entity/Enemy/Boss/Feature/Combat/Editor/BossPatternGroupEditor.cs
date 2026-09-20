using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Alpha.Boss.Editor
{
    // 거리별 객체에서는 중앙 목록의 이름을 선택하고 실제 연결은 고정 ID로 저장한다.
    [CustomEditor(typeof(BossPatternGroup)), CanEditMultipleObjects]
    public sealed class BossPatternGroupEditor : UnityEditor.Editor
    {
        private ReorderableList _patterns;

        private void OnEnable()
        {
            SerializedProperty ids = serializedObject.FindProperty("_patternIds");
            _patterns = new ReorderableList(serializedObject, ids, true, true, true, true);
            _patterns.drawHeaderCallback = p_rect => EditorGUI.LabelField(p_rect, "사용할 패턴");
            _patterns.elementHeight = EditorGUIUtility.singleLineHeight + 4f;
            _patterns.drawElementCallback = DrawPattern;
            _patterns.onAddCallback = p_list =>
            {
                int index = p_list.serializedProperty.arraySize;
                p_list.serializedProperty.InsertArrayElementAtIndex(index);
                p_list.serializedProperty.GetArrayElementAtIndex(index).stringValue = string.Empty;
                p_list.index = index;
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_groupType"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_catalog"));
            if (targets.Length == 1)
                _patterns.DoLayoutList();
            else
                EditorGUILayout.HelpBox("패턴 배정은 그룹 객체 하나를 선택하여 편집하세요.", MessageType.Info);
            serializedObject.ApplyModifiedProperties();
            if (targets.Length != 1)
                return;

            BossPatternGroup group = (BossPatternGroup)target;
            if (group.Catalog == null)
                EditorGUILayout.HelpBox("전체 패턴을 보관하는 BossPatternCatalog를 연결하세요.", MessageType.Warning);
            else
            {
                for (int i = 0; i < group.PatternCount; i++)
                {
                    if (group.GetPattern(i) == null)
                    {
                        EditorGUILayout.HelpBox("비어 있거나 삭제된 패턴, 또는 그룹에 맞지 않는 패턴이 있습니다. AoE에는 Area·Arena를 배정하세요.", MessageType.Warning);
                        break;
                    }
                }
                if (GUILayout.Button("전체 패턴 설정 열기"))
                    Selection.activeObject = group.Catalog.gameObject;
            }
            BossCore boss = group.GetComponentInParent<BossCore>();
            DrawRandomExecutionButtons(boss, group);
            BossPatternEditorGUI.DrawExecutionButtons(boss,
                group.PatternCount, group.GetPattern, group);
        }

        private static void DrawRandomExecutionButtons(BossCore p_boss, BossPatternGroup p_group)
        {
            if (!Application.isPlaying || p_boss == null)
                return;
            using (new EditorGUI.DisabledScope(p_boss.Target == null || !p_group.isActiveAndEnabled ||
                       p_boss.CombatContext.State != EBossCombatState.Idle ||
                       p_boss.EncounterContext.CurrentState != EBossEncounterState.Combat || p_boss.HealthContext.IsDead))
            {
                var types = new HashSet<EBossAttackType>();
                for (int i = 0; i < p_group.PatternCount; i++)
                {
                    BossPatternData pattern = p_group.GetPattern(i);
                    if (pattern == null || !types.Add(pattern.AttackType))
                        continue;
                    if (GUILayout.Button($"거리 조건으로 확률 실행: {pattern.AttackType}") &&
                        !p_boss.TryStartRandomAttack(p_group.GroupType, pattern.AttackType))
                        Debug.LogWarning("현재 거리와 실행 조건에 맞는 패턴이 없거나 공격을 시작하지 못했습니다.", p_group);
                }
            }
        }

        private void DrawPattern(Rect p_rect, int p_index, bool p_active, bool p_focused)
        {
            p_rect.y += 2f;
            p_rect.height = EditorGUIUtility.singleLineHeight;
            SerializedProperty property = _patterns.serializedProperty.GetArrayElementAtIndex(p_index);
            string currentId = property.stringValue;
            BossPatternCatalog catalog = serializedObject.FindProperty("_catalog").objectReferenceValue as BossPatternCatalog;
            if (catalog == null)
                catalog = ((BossPatternGroup)target).Catalog;

            var ids = new List<string> { string.Empty };
            var labels = new List<string> { "선택 안 함" };
            // 삭제된 항목을 첫 번째 패턴으로 바꾸지 않는다. Undo로 원본이 돌아오면 다시 연결된다.
            if (!string.IsNullOrEmpty(currentId) && (catalog == null || catalog.FindPattern(currentId) == null))
            {
                ids.Add(currentId);
                labels.Add("찾을 수 없는 패턴 (다시 선택)");
            }
            if (catalog != null)
            {
                var otherIds = new HashSet<string>();
                for (int i = 0; i < _patterns.serializedProperty.arraySize; i++)
                {
                    if (i != p_index)
                        otherIds.Add(_patterns.serializedProperty.GetArrayElementAtIndex(i).stringValue);
                }
                for (int i = 0; i < catalog.PatternCount; i++)
                {
                    BossPatternData pattern = catalog.GetPattern(i);
                    if (pattern == null || (pattern.Id != currentId &&
                        (otherIds.Contains(pattern.Id) || !((BossPatternGroup)target).SupportsPattern(pattern))))
                        continue;
                    ids.Add(pattern.Id);
                    labels.Add($"{i + 1}. {pattern.PatternName} ({pattern.AttackType})");
                }
            }
            int selected = Mathf.Max(0, ids.IndexOf(currentId));
            EditorGUI.BeginProperty(p_rect, GUIContent.none, property);
            EditorGUI.BeginChangeCheck();
            selected = EditorGUI.Popup(p_rect, selected, labels.ToArray());
            if (EditorGUI.EndChangeCheck())
                property.stringValue = ids[selected];
            EditorGUI.EndProperty();
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        private static void DrawPatternPreviews(BossPatternGroup p_group, GizmoType p_type) =>
            BossPatternEditorGUI.DrawPatternPreviews(p_group.GetComponentInParent<BossCore>(true),
                p_group.transform, p_group.PatternCount, p_group.GetPattern);
    }
}
