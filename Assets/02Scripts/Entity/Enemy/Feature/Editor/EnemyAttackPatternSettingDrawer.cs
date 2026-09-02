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

            DrawSectionHeader(p_position, "Pattern", ref currentY);
            DrawProperty(p_position, p_property, "_patternName", ref currentY);
            DrawProperty(p_position, p_property, "_attackType", ref currentY);
            DrawProperty(p_position, p_property, "_animationIndex", ref currentY);

            SerializedProperty attackType =
                p_property.FindPropertyRelative("_attackType");

            DrawSectionHeader(
                p_position,
                "Animation Timing Events",
                ref currentY);
            DrawProperty(
                p_position,
                p_property,
                "_attackTimings",
                ref currentY);

            DrawSectionHeader(p_position, "Damage", ref currentY);
            DrawProperty(p_position, p_property, "_damageProfile", ref currentY);

            if (attackType != null &&
                !attackType.hasMultipleDifferentValues)
            {
                EEnemyAttackType selectedType =
                    (EEnemyAttackType)attackType.enumValueIndex;

                DrawSectionHeader(
                    p_position,
                    $"{selectedType} Attack",
                    ref currentY);
                DrawTypeProperties(
                    p_position,
                    p_property,
                    selectedType,
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

            AddSectionHeaderHeight(ref height);
            AddPropertyHeight(p_property, "_patternName", ref height);
            AddPropertyHeight(p_property, "_attackType", ref height);
            AddPropertyHeight(p_property, "_animationIndex", ref height);

            SerializedProperty attackType =
                p_property.FindPropertyRelative("_attackType");

            AddSectionHeaderHeight(ref height);
            AddPropertyHeight(p_property, "_attackTimings", ref height);

            AddSectionHeaderHeight(ref height);
            AddPropertyHeight(p_property, "_damageProfile", ref height);

            if (attackType != null &&
                !attackType.hasMultipleDifferentValues)
            {
                AddSectionHeaderHeight(ref height);
                AddTypePropertyHeights(
                    p_property,
                    (EEnemyAttackType)attackType.enumValueIndex,
                    ref height);
            }

            return height;
        }

        private static void DrawSectionHeader(
            Rect p_position,
            string p_title,
            ref float p_currentY)
        {
            Rect headerRect = new(
                p_position.x,
                p_currentY,
                p_position.width,
                EditorGUIUtility.singleLineHeight);

            EditorGUI.LabelField(
                headerRect,
                p_title,
                EditorStyles.boldLabel);

            p_currentY +=
                EditorGUIUtility.singleLineHeight + VerticalSpacing;
        }

        private static void AddSectionHeaderHeight(ref float p_height)
        {
            p_height +=
                VerticalSpacing + EditorGUIUtility.singleLineHeight;
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
                    DrawProperty(p_position, p_property, "_rangeDirectionType", ref p_currentY);
                    DrawProperty(p_position, p_property, "_projectileSpawnPoint", ref p_currentY);
                    DrawProperty(p_position, p_property, "_additionalProjectileSpawnPoints", ref p_currentY);
                    DrawProperty(p_position, p_property, "_projectileMaximumDistance", ref p_currentY);
                    DrawProperty(p_position, p_property, "_projectilePrefab", ref p_currentY);
                    break;

                case EEnemyAttackType.Rush:
                    DrawProperty(p_position, p_property, "_rushSpeed", ref p_currentY);
                    DrawProperty(p_position, p_property, "_rushDistance", ref p_currentY);
                    DrawProperty(p_position, p_property, "_rushArea", ref p_currentY);
                    break;

                case EEnemyAttackType.Area:
                    DrawProperty(
                        p_position,
                        p_property,
                        "_areaAttackArea",
                        ref p_currentY);
                    break;

                case EEnemyAttackType.Arena:
                    DrawProperty(
                        p_position,
                        p_property,
                        "_arenaAttackArea",
                        ref p_currentY);
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
                    AddPropertyHeight(p_property, "_rangeDirectionType", ref p_height);
                    AddPropertyHeight(p_property, "_projectileSpawnPoint", ref p_height);
                    AddPropertyHeight(p_property, "_additionalProjectileSpawnPoints", ref p_height);
                    AddPropertyHeight(p_property, "_projectileMaximumDistance", ref p_height);
                    AddPropertyHeight(p_property, "_projectilePrefab", ref p_height);
                    break;

                case EEnemyAttackType.Rush:
                    AddPropertyHeight(p_property, "_rushSpeed", ref p_height);
                    AddPropertyHeight(p_property, "_rushDistance", ref p_height);
                    AddPropertyHeight(p_property, "_rushArea", ref p_height);
                    break;

                case EEnemyAttackType.Area:
                    AddPropertyHeight(
                        p_property,
                        "_areaAttackArea",
                        ref p_height);
                    break;

                case EEnemyAttackType.Arena:
                    AddPropertyHeight(
                        p_property,
                        "_arenaAttackArea",
                        ref p_height);
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

            if (property == null)
                return;

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

            if (property == null)
                return;

            p_height += VerticalSpacing +
                        EditorGUI.GetPropertyHeight(property, true);
        }

        private static GUIContent CreatePatternLabel(
            SerializedProperty p_property,
            GUIContent p_fallbackLabel)
        {
            SerializedProperty patternNameProperty =
                p_property.FindPropertyRelative("_patternName");

            string patternName = patternNameProperty != null
                ? patternNameProperty.stringValue
                : string.Empty;

            SerializedProperty attackType =
                p_property.FindPropertyRelative("_attackType");

            string typeName = attackType == null
                ? "Unknown"
                : attackType.hasMultipleDifferentValues
                    ? "Mixed"
                    : ((EEnemyAttackType)attackType.enumValueIndex).ToString();

            string prefix = string.IsNullOrWhiteSpace(patternName)
                ? p_fallbackLabel.text
                : patternName;

            return new GUIContent($"{prefix} ({typeName})");
        }
    }
}
