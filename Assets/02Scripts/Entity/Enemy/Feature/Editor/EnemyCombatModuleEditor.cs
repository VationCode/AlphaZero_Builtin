using Alpha.Enemy.View;
using UnityEditor;
using UnityEngine;

namespace Alpha.Enemy.Editor
{
    // Enemy Combat의 전체 Inspector 배치와 안내 표시를 담당한다.
    [CustomEditor(typeof(EnemyCombatModule))]
    public sealed class EnemyCombatModuleEditor : UnityEditor.Editor
    {
        private SerializedProperty _owner;
        private SerializedObject _previewSerializedObject;
        private EnemyAttackPatternListDrawer _patternListDrawer;

        private void OnEnable()
        {
            _owner = serializedObject.FindProperty("_owner");
            SerializedProperty attackPatterns =
                serializedObject.FindProperty("_attackPatterns");

            EnemyAttackAreaPreviewView previewView =
                ((EnemyCombatModule)target)
                .GetComponent<EnemyAttackAreaPreviewView>();

            _previewSerializedObject = previewView != null
                ? new SerializedObject(previewView)
                : null;

            SerializedProperty patternVisibility =
                _previewSerializedObject?.FindProperty(
                    "_patternVisibility");

            _patternListDrawer = new EnemyAttackPatternListDrawer(
                serializedObject,
                attackPatterns,
                patternVisibility);

            Undo.undoRedoPerformed += RepaintSceneView;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= RepaintSceneView;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            _previewSerializedObject?.Update();
            DrawScriptReference();

            EditorGUILayout.PropertyField(_owner);
            EditorGUILayout.Space(2f);

            _patternListDrawer.Draw();
            DrawPatternGuide();

            bool combatChanged =
                serializedObject.ApplyModifiedProperties();

            bool previewChanged =
                _previewSerializedObject?.ApplyModifiedProperties() == true;

            if (combatChanged || previewChanged)
                RepaintSceneView();
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

        // 패턴 값을 조절하는 동안 Scene View의 공격 범위를 즉시 갱신한다.
        private static void RepaintSceneView()
        {
            SceneView.RepaintAll();
        }
    }
}
