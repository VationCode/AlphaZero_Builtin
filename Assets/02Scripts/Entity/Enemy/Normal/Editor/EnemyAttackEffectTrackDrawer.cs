using Alpha.Enemy.Effect;
using UnityEditor;
using UnityEngine;

namespace Alpha.Enemy.Editor
{
    // Effect Track을 공격 타입과 AnimationIndex가 드러나는 Foldout으로 표시한다.
    [CustomPropertyDrawer(typeof(EnemyAttackEffectTrack))]
    public sealed class EnemyAttackEffectTrackDrawer : PropertyDrawer
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
                "_attackType",
                "Attack Type",
                ref currentY);
            DrawProperty(
                p_position,
                p_property,
                "_animationIndex",
                "Animation Index",
                ref currentY);
            DrawProperty(
                p_position,
                p_property,
                "_effectTimings",
                "Effect Timings",
                ref currentY);

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
            AddPropertyHeight(p_property, "_attackType", ref height);
            AddPropertyHeight(p_property, "_animationIndex", ref height);
            AddPropertyHeight(p_property, "_effectTimings", ref height);
            return height;
        }

        private static GUIContent CreateLabel(
            SerializedProperty p_property,
            GUIContent p_fallback)
        {
            SerializedProperty attackType =
                p_property.FindPropertyRelative("_attackType");
            SerializedProperty animationIndex =
                p_property.FindPropertyRelative("_animationIndex");

            if (attackType == null || animationIndex == null)
                return p_fallback;

            string attackName =
                ((EEnemyAttackType)attackType.enumValueIndex).ToString();
            string indexName = animationIndex.intValue >= 0
                ? animationIndex.intValue.ToString()
                : "Default";

            return new GUIContent($"{attackName} [{indexName}]");
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
