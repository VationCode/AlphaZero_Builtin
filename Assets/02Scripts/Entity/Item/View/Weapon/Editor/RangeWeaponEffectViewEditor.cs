using UnityEditor;
using UnityEngine;

namespace Alpha.Item.Weapon.View.Editor
{
    // 선택한 Tracer Mode에 필요한 효과 설정만 표시한다.
    [CustomEditor(typeof(RangeWeaponEffectView))]
    public sealed class RangeWeaponEffectViewEditor : UnityEditor.Editor
    {
        private const string TracerModePropertyName = "_tracerMode";
        private const string TracerPropertyName = "_bulletTracerPrefab";

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty property =
                serializedObject.GetIterator();

            bool enterChildren = true;

            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (property.propertyPath == "m_Script")
                {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.PropertyField(property, true);

                    continue;
                }

                if (property.propertyPath == TracerPropertyName)
                {
                    DrawTracerSetting(property);
                    continue;
                }

                EditorGUILayout.PropertyField(property, true);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawTracerSetting(
            SerializedProperty p_tracerProperty)
        {
            SerializedProperty tracerMode =
                serializedObject.FindProperty(TracerModePropertyName);

            if (tracerMode == null ||
                (ERangeTracerMode)tracerMode.enumValueIndex !=
                ERangeTracerMode.Hitscan)
                return;

            EditorGUILayout.PropertyField(
                p_tracerProperty,
                new GUIContent(
                    "Hitscan Tracer Prefab",
                    "Hitscan 공격의 시작점부터 즉시 판정된 끝점까지 표현할 Tracer입니다."));
        }
    }
}
