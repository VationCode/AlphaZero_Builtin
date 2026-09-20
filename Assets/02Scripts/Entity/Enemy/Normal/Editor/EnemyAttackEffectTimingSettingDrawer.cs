using Alpha.Enemy.Effect;
using UnityEditor;
using UnityEngine;

namespace Alpha.Enemy.Editor
{
    // Effect Prefab과 실제 재생 구간이 한눈에 보이도록 표시한다.
    [CustomPropertyDrawer(typeof(EnemyAttackEffectTimingSetting))]
    public sealed class EnemyAttackEffectTimingSettingDrawer : PropertyDrawer
    {
        private static readonly string[] PropertyNames =
        {
            "_effectPrefab",
            "_spawnPoint",
            "_followSpawnPoint",
            "_startTimeSeconds",
            "_endTimeSeconds",
            "_tailDuration"
        };

        private const float VerticalSpacing = 2f;

        public override void OnGUI(
            Rect p_position,
            SerializedProperty p_property,
            GUIContent p_label)
        {
            EditorGUI.BeginProperty(p_position, p_label, p_property);

            Rect foldoutRect = new(
                p_position.x,
                p_position.y,
                p_position.width,
                EditorGUIUtility.singleLineHeight);

            p_property.isExpanded = EditorGUI.Foldout(
                foldoutRect,
                p_property.isExpanded,
                CreateLabel(p_property, p_label),
                true);

            if (!p_property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            float currentY = foldoutRect.yMax + VerticalSpacing;
            EditorGUI.indentLevel++;

            for (int index = 0;
                 index < PropertyNames.Length;
                 index++)
            {
                SerializedProperty property =
                    p_property.FindPropertyRelative(PropertyNames[index]);

                if (property == null)
                    continue;

                float height = EditorGUI.GetPropertyHeight(property, true);
                Rect propertyRect = new(
                    p_position.x,
                    currentY,
                    p_position.width,
                    height);

                EditorGUI.PropertyField(propertyRect, property, true);
                currentY += height + VerticalSpacing;
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(
            SerializedProperty p_property,
            GUIContent p_label)
        {
            if (!p_property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            float height = EditorGUIUtility.singleLineHeight;

            for (int index = 0;
                 index < PropertyNames.Length;
                 index++)
            {
                SerializedProperty property =
                    p_property.FindPropertyRelative(PropertyNames[index]);

                if (property == null)
                    continue;

                height += VerticalSpacing +
                          EditorGUI.GetPropertyHeight(property, true);
            }

            return height;
        }

        private static GUIContent CreateLabel(
            SerializedProperty p_property,
            GUIContent p_fallback)
        {
            SerializedProperty prefab =
                p_property.FindPropertyRelative("_effectPrefab");
            SerializedProperty startTime =
                p_property.FindPropertyRelative("_startTimeSeconds");
            SerializedProperty endTime =
                p_property.FindPropertyRelative("_endTimeSeconds");

            if (prefab == null || startTime == null || endTime == null)
                return p_fallback;

            string effectName = prefab.objectReferenceValue != null
                ? prefab.objectReferenceValue.name
                : "Effect";

            return new GUIContent(
                $"{effectName} " +
                $"{startTime.floatValue:0.00}s" +
                $" - {endTime.floatValue:0.00}s");
        }
    }
}
