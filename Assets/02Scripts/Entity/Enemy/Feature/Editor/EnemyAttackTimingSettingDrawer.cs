using UnityEditor;
using UnityEngine;

namespace Alpha.Enemy.Editor
{
    // 타이밍 종류에 따라 필요한 Projectile 또는 Collider 설정만 표시한다.
    [CustomPropertyDrawer(typeof(EnemyAttackTimingSetting))]
    public sealed class EnemyAttackTimingSettingDrawer : PropertyDrawer
    {
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

            DrawProperty(
                p_position,
                p_property,
                "_eventType",
                "Event Type",
                ref currentY);
            DrawProperty(
                p_position,
                p_property,
                "_startNormalizedTime",
                "Start Normalized Time",
                ref currentY);

            SerializedProperty eventType =
                p_property.FindPropertyRelative("_eventType");

            if (eventType != null &&
                !eventType.hasMultipleDifferentValues &&
                (EEnemyAttackTimingType)eventType.enumValueIndex ==
                EEnemyAttackTimingType.Collider)
            {
                DrawProperty(
                    p_position,
                    p_property,
                    "_attackCollider",
                    "Attack Collider",
                    ref currentY);
                DrawProperty(
                    p_position,
                    p_property,
                    "_endNormalizedTime",
                    "End Normalized Time",
                    ref currentY);
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

            AddPropertyHeight(p_property, "_eventType", ref height);
            AddPropertyHeight(
                p_property,
                "_startNormalizedTime",
                ref height);

            SerializedProperty eventType =
                p_property.FindPropertyRelative("_eventType");

            if (eventType != null &&
                !eventType.hasMultipleDifferentValues &&
                (EEnemyAttackTimingType)eventType.enumValueIndex ==
                EEnemyAttackTimingType.Collider)
            {
                AddPropertyHeight(
                    p_property,
                    "_attackCollider",
                    ref height);
                AddPropertyHeight(
                    p_property,
                    "_endNormalizedTime",
                    ref height);
            }

            return height;
        }

        private static GUIContent CreateLabel(
            SerializedProperty p_property,
            GUIContent p_fallback)
        {
            SerializedProperty eventType =
                p_property.FindPropertyRelative("_eventType");
            SerializedProperty startTime =
                p_property.FindPropertyRelative(
                    "_startNormalizedTime");

            if (eventType == null || startTime == null)
                return p_fallback;

            string eventName =
                ((EEnemyAttackTimingType)eventType.enumValueIndex)
                .ToString();

            return new GUIContent(
                $"{eventName} @ {startTime.floatValue:0.00}");
        }

        private static void DrawProperty(
            Rect p_position,
            SerializedProperty p_parent,
            string p_relativeName,
            string p_label,
            ref float p_currentY)
        {
            SerializedProperty property =
                p_parent.FindPropertyRelative(p_relativeName);

            if (property == null)
                return;

            float height = EditorGUI.GetPropertyHeight(property, true);
            Rect propertyRect = new(
                p_position.x,
                p_currentY,
                p_position.width,
                height);

            EditorGUI.PropertyField(
                propertyRect,
                property,
                new GUIContent(p_label),
                true);
            p_currentY += height + VerticalSpacing;
        }

        private static void AddPropertyHeight(
            SerializedProperty p_parent,
            string p_relativeName,
            ref float p_height)
        {
            SerializedProperty property =
                p_parent.FindPropertyRelative(p_relativeName);

            if (property == null)
                return;

            p_height += VerticalSpacing +
                        EditorGUI.GetPropertyHeight(property, true);
        }
    }
}
