using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Alpha.AlphaCamera.Editor
{
    // 각 Camera Shake preset 설정 바로 아래에서 플레이 모드 테스트를 제공한다.
    [CustomEditor(typeof(CameraShakeModule))]
    public sealed class CameraShakeModuleEditor : UnityEditor.Editor
    {
        private const float ElementPadding = 6f;
        private const float ButtonSpacing = 2f;

        private SerializedProperty _shakeRoot;
        private SerializedProperty _presets;
        private SerializedProperty _envelope;
        private ReorderableList _presetList;

        private void OnEnable()
        {
            _shakeRoot = serializedObject.FindProperty("_shakeRoot");
            _presets = serializedObject.FindProperty("_presets");
            _envelope = serializedObject.FindProperty("_envelope");

            _presetList = new ReorderableList(
                serializedObject,
                _presets,
                true,
                true,
                true,
                true);

            _presetList.drawHeaderCallback = p_rect =>
                EditorGUI.LabelField(p_rect, "Shake Presets");

            _presetList.elementHeightCallback = GetElementHeight;
            _presetList.drawElementCallback = DrawPresetElement;
            _presetList.onAddCallback = AddPreset;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    "Script",
                    MonoScript.FromMonoBehaviour(
                        (CameraShakeModule)target),
                    typeof(CameraShakeModule),
                    false);
            }

            EditorGUILayout.PropertyField(_shakeRoot);
            EditorGUILayout.Space(2f);

            _presetList.DoLayoutList();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "각 preset의 Test 버튼은 플레이 모드에서 사용할 수 있습니다.",
                    MessageType.Info);
            }

            EditorGUILayout.PropertyField(_envelope);

            serializedObject.ApplyModifiedProperties();
        }

        private float GetElementHeight(int p_index)
        {
            SerializedProperty element =
                _presets.GetArrayElementAtIndex(p_index);

            return EditorGUI.GetPropertyHeight(element, true) +
                   ButtonSpacing +
                   EditorGUIUtility.singleLineHeight +
                   ElementPadding;
        }

        private void DrawPresetElement(
            Rect p_rect,
            int p_index,
            bool p_isActive,
            bool p_isFocused)
        {
            SerializedProperty element =
                _presets.GetArrayElementAtIndex(p_index);
            float propertyHeight =
                EditorGUI.GetPropertyHeight(element, true);

            Rect propertyRect = new(
                p_rect.x,
                p_rect.y + 2f,
                p_rect.width,
                propertyHeight);

            EditorGUI.PropertyField(
                propertyRect,
                element,
                new GUIContent($"Preset {p_index + 1}"),
                true);

            SerializedProperty nameProperty =
                element.FindPropertyRelative("_name");
            string presetName = nameProperty?.stringValue;

            Rect buttonRect = new(
                p_rect.x,
                propertyRect.yMax + ButtonSpacing,
                p_rect.width,
                EditorGUIUtility.singleLineHeight);

            bool canTest =
                Application.isPlaying &&
                !string.IsNullOrWhiteSpace(presetName);

            using (new EditorGUI.DisabledScope(!canTest))
            {
                string buttonLabel = string.IsNullOrWhiteSpace(presetName)
                    ? "Test Shake"
                    : $"Test {presetName}";

                if (GUI.Button(buttonRect, buttonLabel))
                    PlayPreset(presetName);
            }
        }

        private void AddPreset(ReorderableList p_list)
        {
            int newIndex = _presets.arraySize;
            _presets.arraySize++;

            SerializedProperty preset =
                _presets.GetArrayElementAtIndex(newIndex);
            SerializedProperty setting =
                preset.FindPropertyRelative("_setting");

            preset.FindPropertyRelative("_name").stringValue =
                $"Shake{newIndex + 1}";
            setting.FindPropertyRelative("_duration").floatValue = 0.25f;
            setting.FindPropertyRelative("_horizontalAmplitude").floatValue = 0.01f;
            setting.FindPropertyRelative("_verticalAmplitude").floatValue = 0.01f;
            setting.FindPropertyRelative("_yawAngle").floatValue = 0.25f;
            setting.FindPropertyRelative("_rollAngle").floatValue = 0.25f;
            setting.FindPropertyRelative("_frequency").floatValue = 25f;
        }

        private void PlayPreset(string p_presetName)
        {
            serializedObject.ApplyModifiedProperties();

            foreach (Object targetObject in targets)
            {
                if (targetObject is CameraShakeModule shakeModule)
                    shakeModule.Play(p_presetName);
            }
        }
    }
}
