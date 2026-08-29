using UnityEditor;
using UnityEngine;

namespace Alpha.Item.Weapon.Range.Editor
{
    // 원거리 무기의 공통 발사 설정을 접을 수 있는 Inspector로 표시한다.
    [CustomPropertyDrawer(typeof(RangeAttackTuning))]
    public sealed class RangeAttackTuningDrawer : PropertyDrawer
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

            DrawProperty(p_position, p_property, "_fireInterval", ref currentY);
            DrawProperty(p_position, p_property, "_projectilesPerShot", ref currentY);
            DrawProperty(p_position, p_property, "_spreadAngle", ref currentY);
            DrawProperty(p_position, p_property, "_recoil", ref currentY);
            DrawProperty(p_position, p_property, "_moveSpeedMultiplier", ref currentY);

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

            AddPropertyHeight(p_property, "_fireInterval", ref height);
            AddPropertyHeight(p_property, "_projectilesPerShot", ref height);
            AddPropertyHeight(p_property, "_spreadAngle", ref height);
            AddPropertyHeight(p_property, "_recoil", ref height);
            AddPropertyHeight(p_property, "_moveSpeedMultiplier", ref height);

            return height;
        }

        private static void DrawProperty(
            Rect p_position,
            SerializedProperty p_parent,
            string p_relativeName,
            ref float p_currentY)
        {
            SerializedProperty property =
                p_parent.FindPropertyRelative(p_relativeName);

            if (property == null)
                return;

            float propertyHeight =
                EditorGUI.GetPropertyHeight(property, true);

            Rect propertyRect = new(
                p_position.x,
                p_currentY,
                p_position.width,
                propertyHeight);

            EditorGUI.PropertyField(propertyRect, property, true);
            p_currentY += propertyHeight + VerticalSpacing;
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
