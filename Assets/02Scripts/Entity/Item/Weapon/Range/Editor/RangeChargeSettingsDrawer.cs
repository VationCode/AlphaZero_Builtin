using UnityEditor;
using UnityEngine;

namespace Alpha.Item.Weapon.Range.Editor
{
    // 차징이 활성화된 경우에만 세부 설정을 Inspector에 표시한다.
    [CustomPropertyDrawer(typeof(RangeChargeSettings))]
    public sealed class RangeChargeSettingsDrawer : PropertyDrawer
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
                p_label,
                true);

            if (!p_property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            float currentY = foldoutRect.yMax + VerticalSpacing;
            EditorGUI.indentLevel++;

            SerializedProperty enabledProperty =
                p_property.FindPropertyRelative("_enabled");

            DrawProperty(p_position, enabledProperty, ref currentY);

            if (ShouldShowDetails(enabledProperty))
            {
                DrawProperty(
                    p_position,
                    p_property.FindPropertyRelative("_maxDuration"),
                    ref currentY);
                DrawProperty(
                    p_position,
                    p_property.FindPropertyRelative("_maxBonusDamage"),
                    ref currentY);
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(
            SerializedProperty p_property,
            GUIContent p_label)
        {
            float height = EditorGUIUtility.singleLineHeight;

            if (!p_property.isExpanded)
                return height;

            SerializedProperty enabledProperty =
                p_property.FindPropertyRelative("_enabled");

            AddPropertyHeight(enabledProperty, ref height);

            if (ShouldShowDetails(enabledProperty))
            {
                AddPropertyHeight(
                    p_property.FindPropertyRelative("_maxDuration"),
                    ref height);
                AddPropertyHeight(
                    p_property.FindPropertyRelative("_maxBonusDamage"),
                    ref height);
            }

            return height;
        }

        private static bool ShouldShowDetails(
            SerializedProperty p_enabledProperty)
        {
            return p_enabledProperty != null &&
                   (p_enabledProperty.hasMultipleDifferentValues ||
                    p_enabledProperty.boolValue);
        }

        private static void DrawProperty(
            Rect p_position,
            SerializedProperty p_property,
            ref float p_currentY)
        {
            if (p_property == null)
                return;

            float propertyHeight =
                EditorGUI.GetPropertyHeight(p_property, true);

            Rect propertyRect = new(
                p_position.x,
                p_currentY,
                p_position.width,
                propertyHeight);

            EditorGUI.PropertyField(propertyRect, p_property, true);
            p_currentY += propertyHeight + VerticalSpacing;
        }

        private static void AddPropertyHeight(
            SerializedProperty p_property,
            ref float p_height)
        {
            if (p_property == null)
                return;

            p_height += VerticalSpacing +
                        EditorGUI.GetPropertyHeight(p_property, true);
        }
    }
}
