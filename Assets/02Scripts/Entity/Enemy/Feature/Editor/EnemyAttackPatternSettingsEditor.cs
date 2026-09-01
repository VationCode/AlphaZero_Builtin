using Alpha.Enemy.View;
using UnityEditor;
using UnityEngine;

namespace Alpha.Enemy.Editor
{
    // Pattern Settings 컴포넌트의 패턴 원본 목록만 편집한다.
    [CustomEditor(typeof(EnemyAttackPatternSettings))]
    public sealed class EnemyAttackPatternSettingsEditor :
        UnityEditor.Editor
    {
        private SerializedObject _combatSerializedObject;
        private SerializedObject _previewSerializedObject;
        private EnemyAttackPatternListDrawer _patternListDrawer;

        private void OnEnable()
        {
            SerializedProperty patterns =
                serializedObject.FindProperty("_patterns");

            EnemyAttackPatternSettings settings =
                (EnemyAttackPatternSettings)target;
            EnemyCombatModule combat =
                settings.GetComponent<EnemyCombatModule>();
            EnemyAttackAreaPreviewView previewView =
                settings.GetComponent<EnemyAttackAreaPreviewView>();

            _combatSerializedObject = combat != null
                ? new SerializedObject(combat)
                : null;
            _previewSerializedObject = previewView != null
                ? new SerializedObject(previewView)
                : null;

            SerializedProperty distancePatterns =
                _combatSerializedObject?.FindProperty(
                    "_distancePatterns");
            SerializedProperty patternVisibility =
                _previewSerializedObject?.FindProperty(
                    "_attackPatternVisibility");

            _patternListDrawer = new EnemyAttackPatternListDrawer(
                serializedObject,
                patterns,
                distancePatterns,
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
            _combatSerializedObject?.Update();
            _previewSerializedObject?.Update();
            DrawScriptReference();

            _patternListDrawer.Draw();

            bool settingsChanged =
                serializedObject.ApplyModifiedProperties();
            bool combatChanged =
                _combatSerializedObject?.ApplyModifiedProperties() == true;
            bool previewChanged =
                _previewSerializedObject?.ApplyModifiedProperties() == true;

            if (settingsChanged || combatChanged || previewChanged)
                RepaintSceneView();
        }

        private void DrawScriptReference()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    "Script",
                    MonoScript.FromMonoBehaviour(
                        (EnemyAttackPatternSettings)target),
                    typeof(EnemyAttackPatternSettings),
                    false);
            }
        }

        private static void RepaintSceneView()
        {
            SceneView.RepaintAll();
        }
    }
}
