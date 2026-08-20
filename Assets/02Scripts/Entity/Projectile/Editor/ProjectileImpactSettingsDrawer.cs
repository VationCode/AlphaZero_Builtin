using UnityEditor;
using UnityEngine;

namespace Alpha.Projectile.Editor
{
    // Impact Type에 필요한 설정만 중첩 Inspector에 표시한다.
    [CustomPropertyDrawer(typeof(ProjectileImpactSettings))]
    public sealed class ProjectileImpactSettingsDrawer : PropertyDrawer
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

            SerializedProperty impactType =
                p_property.FindPropertyRelative("_impactType");

            DrawProperty(p_position, impactType, ref currentY);

            if (impactType.hasMultipleDifferentValues ||
                (EProjectileImpactType)impactType.enumValueIndex ==
                EProjectileImpactType.Radial)
            {
                DrawProperty(
                    p_position,
                    p_property.FindPropertyRelative("_damageRadius"),
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
            SerializedProperty impactType =
                p_property.FindPropertyRelative("_impactType");

            AddPropertyHeight(impactType, ref height);

            if (impactType.hasMultipleDifferentValues ||
                (EProjectileImpactType)impactType.enumValueIndex ==
                EProjectileImpactType.Radial)
            {
                AddPropertyHeight(
                    p_property.FindPropertyRelative("_damageRadius"),
                    ref height);
            }

            return height;
        }

        private static void DrawProperty(
            Rect p_position,
            SerializedProperty p_property,
            ref float p_currentY)
        {
            float height = EditorGUI.GetPropertyHeight(
                p_property,
                true);

            Rect propertyRect = new(
                p_position.x,
                p_currentY,
                p_position.width,
                height);

            EditorGUI.PropertyField(propertyRect, p_property, true);
            p_currentY += height + VerticalSpacing;
        }

        private static void AddPropertyHeight(
            SerializedProperty p_property,
            ref float p_height)
        {
            p_height += VerticalSpacing +
                        EditorGUI.GetPropertyHeight(p_property, true);
        }
    }
}
