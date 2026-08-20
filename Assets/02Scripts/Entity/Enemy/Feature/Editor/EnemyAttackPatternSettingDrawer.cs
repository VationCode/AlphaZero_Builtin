using UnityEditor;
using UnityEngine;

namespace Alpha.Enemy.Editor
{
    // 공격 타입에 필요한 설정만 각 패턴의 중첩 Inspector에 표시한다.
    [CustomPropertyDrawer(typeof(EnemyAttackPatternSetting))]
    public sealed class EnemyAttackPatternSettingDrawer : PropertyDrawer
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
                CreatePatternLabel(p_property, p_label),
                true);

            if (!p_property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            float currentY = foldoutRect.yMax + VerticalSpacing;
            EditorGUI.indentLevel++;

            DrawProperty(p_position, p_property, "_patternName", ref currentY);
            DrawProperty(p_position, p_property, "_attackType", ref currentY);

            SerializedProperty attackType =
                p_property.FindPropertyRelative("_attackType");

            DrawProperty(p_position, p_property, "_minimumDistance", ref currentY);

            if (attackType.hasMultipleDifferentValues ||
                (EEnemyAttackType)attackType.enumValueIndex !=
                EEnemyAttackType.Melee)
            {
                DrawProperty(
                    p_position,
                    p_property,
                    "_maximumDistance",
                    ref currentY);
            }

            DrawProperty(p_position, p_property, "_cooldown", ref currentY);
            DrawProperty(p_position, p_property, "_selectionWeight", ref currentY);
            DrawProperty(p_position, p_property, "_animationIndex", ref currentY);
            DrawProperty(p_position, p_property, "_windupDuration", ref currentY);
            DrawProperty(p_position, p_property, "_recoveryDuration", ref currentY);
            DrawProperty(p_position, p_property, "_damageProfile", ref currentY);

            if (!attackType.hasMultipleDifferentValues)
            {
                DrawTypeProperties(
                    p_position,
                    p_property,
                    (EEnemyAttackType)attackType.enumValueIndex,
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

            AddPropertyHeight(p_property, "_patternName", ref height);
            AddPropertyHeight(p_property, "_attackType", ref height);

            SerializedProperty attackType =
                p_property.FindPropertyRelative("_attackType");

            AddPropertyHeight(p_property, "_minimumDistance", ref height);

            if (attackType.hasMultipleDifferentValues ||
                (EEnemyAttackType)attackType.enumValueIndex !=
                EEnemyAttackType.Melee)
            {
                AddPropertyHeight(
                    p_property,
                    "_maximumDistance",
                    ref height);
            }

            AddPropertyHeight(p_property, "_cooldown", ref height);
            AddPropertyHeight(p_property, "_selectionWeight", ref height);
            AddPropertyHeight(p_property, "_animationIndex", ref height);
            AddPropertyHeight(p_property, "_windupDuration", ref height);
            AddPropertyHeight(p_property, "_recoveryDuration", ref height);
            AddPropertyHeight(p_property, "_damageProfile", ref height);

            if (!attackType.hasMultipleDifferentValues)
            {
                AddTypePropertyHeights(
                    p_property,
                    (EEnemyAttackType)attackType.enumValueIndex,
                    ref height);
            }

            return height;
        }

        private static void DrawTypeProperties(
            Rect p_position,
            SerializedProperty p_property,
            EEnemyAttackType p_attackType,
            ref float p_currentY)
        {
            switch (p_attackType)
            {
                case EEnemyAttackType.Melee:
                    DrawProperty(
                        p_position,
                        p_property,
                        "_meleeArea",
                        ref p_currentY);
                    break;

                case EEnemyAttackType.Range:
                    DrawProperty(p_position, p_property, "_projectileSpawnPoint", ref p_currentY);
                    DrawProperty(p_position, p_property, "_projectileLaunchSettings", ref p_currentY);
                    break;

                case EEnemyAttackType.Rush:
                    DrawProperty(p_position, p_property, "_rushSpeed", ref p_currentY);
                    DrawProperty(p_position, p_property, "_rushDistance", ref p_currentY);
                    DrawProperty(p_position, p_property, "_rushDuration", ref p_currentY);
                    DrawProperty(p_position, p_property, "_rushArea", ref p_currentY);
                    break;
            }
        }

        private static void AddTypePropertyHeights(
            SerializedProperty p_property,
            EEnemyAttackType p_attackType,
            ref float p_height)
        {
            switch (p_attackType)
            {
                case EEnemyAttackType.Melee:
                    AddPropertyHeight(p_property, "_meleeArea", ref p_height);
                    break;

                case EEnemyAttackType.Range:
                    AddPropertyHeight(p_property, "_projectileSpawnPoint", ref p_height);
                    AddPropertyHeight(p_property, "_projectileLaunchSettings", ref p_height);
                    break;

                case EEnemyAttackType.Rush:
                    AddPropertyHeight(p_property, "_rushSpeed", ref p_height);
                    AddPropertyHeight(p_property, "_rushDistance", ref p_height);
                    AddPropertyHeight(p_property, "_rushDuration", ref p_height);
                    AddPropertyHeight(p_property, "_rushArea", ref p_height);
                    break;
            }
        }

        private static void DrawProperty(
            Rect p_position,
            SerializedProperty p_parent,
            string p_relativeName,
            ref float p_currentY)
        {
            SerializedProperty property =
                p_parent.FindPropertyRelative(p_relativeName);

            float propertyHeight = EditorGUI.GetPropertyHeight(
                property,
                true);

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

            p_height += VerticalSpacing +
                        EditorGUI.GetPropertyHeight(property, true);
        }

        private static GUIContent CreatePatternLabel(
            SerializedProperty p_property,
            GUIContent p_fallbackLabel)
        {
            string patternName = p_property
                .FindPropertyRelative("_patternName")
                .stringValue;

            SerializedProperty attackType =
                p_property.FindPropertyRelative("_attackType");

            string typeName = attackType.hasMultipleDifferentValues
                ? "Mixed"
                : ((EEnemyAttackType)attackType.enumValueIndex).ToString();

            string prefix = string.IsNullOrWhiteSpace(patternName)
                ? p_fallbackLabel.text
                : patternName;

            return new GUIContent($"{prefix} ({typeName})");
        }
    }
}
