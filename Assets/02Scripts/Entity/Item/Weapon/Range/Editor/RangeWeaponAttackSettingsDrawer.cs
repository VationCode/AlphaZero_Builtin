using UnityEditor;
using UnityEngine;

namespace Alpha.Item.Weapon.Range.Editor
{
    // 공격 공통값과 선택한 방식의 상세값을 제목 기준으로 평탄하게 표시한다.
    [CustomPropertyDrawer(typeof(RangeWeaponAttackSettings))]
    public sealed class RangeWeaponAttackSettingsDrawer : PropertyDrawer
    {
        private const float VerticalSpacing = 2f;

        private static readonly GUIContent AttackTypeTitle =
            new("[ 공격 방식 ]");
        private static readonly GUIContent AttackDetailTitle =
            new("[ 공격 상세 ]");

        private static readonly string[] PenetrationPropertyNames =
        {
            "_startRadius",
            "_endRadius"
        };

        private static readonly string[] ProjectilePropertyNames =
        {
            "_prefab",
            "_speed"
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

            SerializedProperty attackType =
                p_property.FindPropertyRelative("_attackType");
            SerializedProperty hitMask =
                p_property.FindPropertyRelative("_hitMask");
            SerializedProperty activeSettings =
                p_property.FindPropertyRelative("_activeSettings");
            float currentY = foldoutRect.yMax + VerticalSpacing;

            EditorGUI.indentLevel++;
            DrawTitle(p_position, AttackTypeTitle, ref currentY);

            EditorGUI.BeginChangeCheck();
            DrawProperty(p_position, attackType, ref currentY);
            bool didChangeType = EditorGUI.EndChangeCheck();

            DrawProperty(p_position, hitMask, ref currentY);

            if (!attackType.hasMultipleDifferentValues)
            {
                ERangeAttackType selectedType =
                    (ERangeAttackType)attackType.enumValueIndex;

                if (didChangeType ||
                    !MatchesType(activeSettings, selectedType))
                {
                    activeSettings.managedReferenceValue =
                        RangeWeaponAttackSettings.CreateDefault(
                            selectedType);
                }

                string[] detailPropertyNames =
                    ResolveDetailPropertyNames(selectedType);

                if (detailPropertyNames.Length > 0 &&
                    activeSettings.managedReferenceValue != null)
                {
                    DrawTitle(
                        p_position,
                        AttackDetailTitle,
                        ref currentY);

                    foreach (string propertyName in detailPropertyNames)
                    {
                        DrawProperty(
                            p_position,
                            activeSettings.FindPropertyRelative(
                                propertyName),
                            ref currentY);
                    }
                }
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

            SerializedProperty attackType =
                p_property.FindPropertyRelative("_attackType");
            SerializedProperty hitMask =
                p_property.FindPropertyRelative("_hitMask");
            SerializedProperty activeSettings =
                p_property.FindPropertyRelative("_activeSettings");

            AddTitleHeight(ref height);
            AddPropertyHeight(attackType, ref height);
            AddPropertyHeight(hitMask, ref height);

            if (attackType == null ||
                attackType.hasMultipleDifferentValues ||
                activeSettings?.managedReferenceValue == null)
            {
                return height;
            }

            string[] detailPropertyNames = ResolveDetailPropertyNames(
                (ERangeAttackType)attackType.enumValueIndex);

            if (detailPropertyNames.Length == 0)
                return height;

            AddTitleHeight(ref height);

            foreach (string propertyName in detailPropertyNames)
            {
                AddPropertyHeight(
                    activeSettings.FindPropertyRelative(propertyName),
                    ref height);
            }

            return height;
        }

        private static string[] ResolveDetailPropertyNames(
            ERangeAttackType p_attackType)
        {
            return p_attackType switch
            {
                ERangeAttackType.Penetration =>
                    PenetrationPropertyNames,
                ERangeAttackType.Projectile =>
                    ProjectilePropertyNames,
                _ => System.Array.Empty<string>()
            };
        }

        private static bool MatchesType(
            SerializedProperty p_activeSettings,
            ERangeAttackType p_attackType)
        {
            if (p_attackType is ERangeAttackType.None or
                ERangeAttackType.Hitscan)
            {
                return p_activeSettings.managedReferenceValue == null;
            }

            return p_activeSettings.managedReferenceValue is
                       RangeAttackSettings settings &&
                   settings.AttackType == p_attackType;
        }

        private static void DrawTitle(
            Rect p_position,
            GUIContent p_title,
            ref float p_currentY)
        {
            Rect titleRect = new(
                p_position.x,
                p_currentY,
                p_position.width,
                EditorGUIUtility.singleLineHeight);

            EditorGUI.LabelField(titleRect, p_title, EditorStyles.boldLabel);
            p_currentY = titleRect.yMax + VerticalSpacing;
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
            p_currentY = propertyRect.yMax + VerticalSpacing;
        }

        private static void AddTitleHeight(ref float p_height)
        {
            p_height += VerticalSpacing +
                        EditorGUIUtility.singleLineHeight;
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
