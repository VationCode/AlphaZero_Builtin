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
        private SerializedProperty _patternSettings;
        private SerializedProperty _distancePatterns;
        private SerializedObject _previewSerializedObject;
        private SerializedObject _patternSettingsSerializedObject;
        private EnemyDistancePatternListDrawer _distancePatternListDrawer;

        private void OnEnable()
        {
            _owner = serializedObject.FindProperty("_owner");
            _patternSettings =
                serializedObject.FindProperty("_patternSettings");
            _distancePatterns =
                serializedObject.FindProperty("_distancePatterns");

            EnemyAttackAreaPreviewView previewView =
                ((EnemyCombatModule)target)
                .GetComponent<EnemyAttackAreaPreviewView>();

            _previewSerializedObject = previewView != null
                ? new SerializedObject(previewView)
                : null;

            RebuildDistancePatternDrawer();

            Undo.undoRedoPerformed += RepaintSceneView;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= RepaintSceneView;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            _patternSettingsSerializedObject?.Update();
            _previewSerializedObject?.Update();
            DrawScriptReference();

            EditorGUILayout.PropertyField(_owner);
            EditorGUILayout.Space(2f);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_patternSettings);
            bool patternSettingsChanged = EditorGUI.EndChangeCheck();

            if (patternSettingsChanged)
            {
                serializedObject.ApplyModifiedProperties();
                RebuildDistancePatternDrawer();
                serializedObject.Update();
                _patternSettingsSerializedObject?.Update();
            }

            if (_patternSettings.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "Pattern Settings 컴포넌트를 연결해야 " +
                    "거리별 패턴을 선택할 수 있습니다.",
                    MessageType.Warning);
            }

            EditorGUILayout.Space(6f);
            _distancePatternListDrawer.Draw();
            DrawPatternGuide();

            bool combatChanged =
                serializedObject.ApplyModifiedProperties();

            bool previewChanged =
                _previewSerializedObject?.ApplyModifiedProperties() == true;

            _patternSettingsSerializedObject?.ApplyModifiedProperties();

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
                "Distance Patterns에서 현재 거리와 일치하는 규칙을 찾고, " +
                "연결된 Pattern Settings를 실행합니다. 후보가 여러 개면 " +
                "Selection Weight 비율로 하나를 선택합니다.",
                MessageType.Info);
        }

        private void RebuildDistancePatternDrawer()
        {
            EnemyAttackPatternSettings patternSettings =
                _patternSettings?.objectReferenceValue as
                    EnemyAttackPatternSettings;

            _patternSettingsSerializedObject = patternSettings != null
                ? new SerializedObject(patternSettings)
                : null;

            SerializedProperty patterns =
                _patternSettingsSerializedObject?.FindProperty(
                    "_patterns");

            SerializedProperty patternVisibility =
                _previewSerializedObject?.FindProperty(
                    "_distancePatternVisibility");

            _distancePatternListDrawer =
                new EnemyDistancePatternListDrawer(
                    serializedObject,
                    _distancePatterns,
                    patterns,
                    patternVisibility);
        }

        // 패턴 값을 조절하는 동안 Scene View의 공격 범위를 즉시 갱신한다.
        private static void RepaintSceneView()
        {
            SceneView.RepaintAll();
        }
    }
}
