using UnityEditor;
using UnityEngine;

namespace Alpha.Enemy.Editor
{
    // Enemy Combat의 전체 Inspector 배치와 안내 표시를 담당한다.
    [CustomEditor(typeof(EnemyCombatModule))]
    public sealed class EnemyCombatModuleEditor : UnityEditor.Editor
    {
        private SerializedProperty _owner;
        private EnemyAttackPatternListDrawer _patternListDrawer;

        private void OnEnable()
        {
            _owner = serializedObject.FindProperty("_owner");
            SerializedProperty attackPatterns =
                serializedObject.FindProperty("_attackPatterns");

            _patternListDrawer = new EnemyAttackPatternListDrawer(
                serializedObject,
                attackPatterns);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawScriptReference();

            EditorGUILayout.PropertyField(_owner);
            EditorGUILayout.Space(2f);

            _patternListDrawer.Draw();
            DrawPatternGuide();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawScriptReference()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    "Script",
                    MonoScript.FromMonoBehaviour(
                        (EnemyCombatModule)target),
                    typeof(EnemyCombatModule),
                    false);
            }
        }

        private static void DrawPatternGuide()
        {
            EditorGUILayout.HelpBox(
                "거리·쿨타임 조건을 만족하는 패턴 중 " +
                "Selection Weight 비율로 하나를 선택합니다.",
                MessageType.Info);
        }
    }
}
