using UnityEditor;
using UnityEngine;

namespace Alpha.Item.Weapon.Range.Editor
{
    // 선택한 무기 종류에 필요한 공격 설정만 Inspector에 표시한다.
    [CustomPropertyDrawer(typeof(RangeWeaponSettings))]
    public sealed class RangeWeaponSettingsDrawer : PropertyDrawer
    {
        private const float VerticalSpacing = 2f;

        private static readonly string[] CommonPropertyNames =
        {
            "_weaponType",
            "_baseDamage",
            "_maxDistance",
            "_attackTuning",
            "_defaultTriggerMode",
            "_aimView",
            "_chargeSettings",
            "_impactSettings"
        };

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

            foreach (string propertyName in CommonPropertyNames)
            {
                DrawProperty(
                    p_position,
                    p_property.FindPropertyRelative(propertyName),
                    ref currentY);
            }

            DrawAttackSpecificProperties(
                p_position,
                p_property,
                ref currentY);

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

            foreach (string propertyName in CommonPropertyNames)
            {
                AddPropertyHeight(
                    p_property.FindPropertyRelative(propertyName),
                    ref height);
            }

            AddAttackSpecificPropertyHeights(p_property, ref height);
            return height;
        }

        private static void DrawAttackSpecificProperties(
            Rect p_position,
            SerializedProperty p_property,
            ref float p_currentY)
        {
            SerializedProperty weaponType =
                p_property.FindPropertyRelative("_weaponType");

            if (weaponType == null)
                return;

            if (weaponType.hasMultipleDifferentValues)
            {
                DrawProperty(
                    p_position,
                    p_property.FindPropertyRelative("_physics"),
                    ref p_currentY);
                DrawProperty(
                    p_position,
                    p_property.FindPropertyRelative("_penetration"),
                    ref p_currentY);
                DrawProperty(
                    p_position,
                    p_property.FindPropertyRelative("_projectile"),
                    ref p_currentY);
                return;
            }

            ERangeAttackType attackType =
                RangeWeaponSettings.ResolveAttackType(
                    (ERangeWeaponType)weaponType.intValue);

            if (attackType == ERangeAttackType.Hitscan ||
                attackType == ERangeAttackType.Penetration)
            {
                DrawProperty(
                    p_position,
                    p_property.FindPropertyRelative("_physics"),
                    ref p_currentY);
            }

            if (attackType == ERangeAttackType.Penetration)
            {
                DrawProperty(
                    p_position,
                    p_property.FindPropertyRelative("_penetration"),
                    ref p_currentY);
            }

            if (attackType == ERangeAttackType.Projectile)
            {
                DrawProperty(
                    p_position,
                    p_property.FindPropertyRelative("_projectile"),
                    ref p_currentY);
            }
        }

        private static void AddAttackSpecificPropertyHeights(
            SerializedProperty p_property,
            ref float p_height)
        {
            SerializedProperty weaponType =
                p_property.FindPropertyRelative("_weaponType");

            if (weaponType == null)
                return;

            if (weaponType.hasMultipleDifferentValues)
            {
                AddPropertyHeight(
                    p_property.FindPropertyRelative("_physics"),
                    ref p_height);
                AddPropertyHeight(
                    p_property.FindPropertyRelative("_penetration"),
                    ref p_height);
                AddPropertyHeight(
                    p_property.FindPropertyRelative("_projectile"),
                    ref p_height);
                return;
            }

            ERangeAttackType attackType =
                RangeWeaponSettings.ResolveAttackType(
                    (ERangeWeaponType)weaponType.intValue);

            if (attackType == ERangeAttackType.Hitscan ||
                attackType == ERangeAttackType.Penetration)
            {
                AddPropertyHeight(
                    p_property.FindPropertyRelative("_physics"),
                    ref p_height);
            }

            if (attackType == ERangeAttackType.Penetration)
            {
                AddPropertyHeight(
                    p_property.FindPropertyRelative("_penetration"),
                    ref p_height);
            }

            if (attackType == ERangeAttackType.Projectile)
            {
                AddPropertyHeight(
                    p_property.FindPropertyRelative("_projectile"),
                    ref p_height);
            }
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
