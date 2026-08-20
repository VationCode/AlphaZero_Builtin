using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Alpha.Enemy.Editor
{
    // 공격 패턴을 1~2개로 제한하고 타입별 설정 Drawer와 함께 표시한다.
    [CustomEditor(typeof(EnemyCombatModule))]
    public sealed class EnemyCombatModuleEditor : UnityEditor.Editor
    {
        private SerializedProperty _owner;
        private SerializedProperty _attackPatterns;
        private ReorderableList _patternList;

        private void OnEnable()
        {
            _owner = serializedObject.FindProperty("_owner");
            _attackPatterns =
                serializedObject.FindProperty("_attackPatterns");

            _patternList = new ReorderableList(
                serializedObject,
                _attackPatterns,
                true,
                true,
                true,
                true);

            _patternList.drawHeaderCallback = p_rect =>
                EditorGUI.LabelField(p_rect, "Attack Patterns (1-2)");

            _patternList.elementHeightCallback = p_index =>
                EditorGUI.GetPropertyHeight(
                    _attackPatterns.GetArrayElementAtIndex(p_index),
                    true) + 4f;

            _patternList.drawElementCallback =
                (p_rect, p_index, p_active, p_focused) =>
                {
                    SerializedProperty element =
                        _attackPatterns.GetArrayElementAtIndex(p_index);

                    p_rect.y += 2f;
                    p_rect.height = EditorGUI.GetPropertyHeight(
                        element,
                        true);

                    EditorGUI.PropertyField(
                        p_rect,
                        element,
                        new GUIContent($"Pattern {p_index + 1}"),
                        true);
                };

            _patternList.onCanAddCallback = p_list =>
                _attackPatterns.arraySize <
                EnemyCombatModule.MaximumPatternCount;

            _patternList.onCanRemoveCallback = p_list =>
                _attackPatterns.arraySize >
                EnemyCombatModule.MinimumPatternCount;

            _patternList.onAddCallback = p_list =>
            {
                if (_attackPatterns.arraySize >=
                    EnemyCombatModule.MaximumPatternCount)
                {
                    return;
                }

                _attackPatterns.InsertArrayElementAtIndex(
                    _attackPatterns.arraySize);
            };

            _patternList.onRemoveCallback = p_list =>
            {
                if (_attackPatterns.arraySize <=
                    EnemyCombatModule.MinimumPatternCount)
                {
                    return;
                }

                ReorderableList.defaultBehaviours.DoRemoveButton(p_list);
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            ClampPatternCount();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    "Script",
                    MonoScript.FromMonoBehaviour(
                        (EnemyCombatModule)target),
                    typeof(EnemyCombatModule),
                    false);
            }

            EditorGUILayout.PropertyField(_owner);
            EditorGUILayout.Space(2f);

            _patternList.DoLayoutList();

            EditorGUILayout.HelpBox(
                "두 패턴이 모두 거리·쿨타임 조건을 만족하면 " +
                "Selection Weight 비율로 하나를 선택합니다.",
                MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }

        private void ClampPatternCount()
        {
            if (_attackPatterns.arraySize <
                EnemyCombatModule.MinimumPatternCount)
            {
                _attackPatterns.InsertArrayElementAtIndex(0);
            }

            if (_attackPatterns.arraySize >
                EnemyCombatModule.MaximumPatternCount)
            {
                _attackPatterns.arraySize =
                    EnemyCombatModule.MaximumPatternCount;
            }
        }
    }
}
