using UnityEditor;
using UnityEngine;

namespace Alpha.Item.Weapon.Range.Editor
{
    // Domain 객체 분리는 유지하고 Inspector에서는 책임 제목 아래에 값을 평탄하게 표시한다.
    [CustomPropertyDrawer(typeof(RangeWeaponSettings))]
    public sealed class RangeWeaponSettingsDrawer : PropertyDrawer
    {
        private const float VerticalSpacing = 2f;

        private static readonly GUIContent BasicTitle = new("[ 기본 ]");
        private static readonly GUIContent ShotTitle = new("[ 발사 ]");
        private static readonly GUIContent FireResponseTitle =
            new("[ 발사 반응 ]");
        private static readonly GUIContent ActionTitle = new("[ 행동 ]");
        private static readonly GUIContent ImpactTitle = new("[ 피격 ]");

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

            DrawTitle(p_position, BasicTitle, ref currentY);
            DrawProperty(
                p_position,
                p_property.FindPropertyRelative("_weaponType"),
                ref currentY);
            DrawProperty(
                p_position,
                p_property.FindPropertyRelative("_baseDamage"),
                ref currentY);
            DrawProperty(
                p_position,
                p_property.FindPropertyRelative("_maxDistance"),
                ref currentY);

            SerializedProperty shotSettings =
                p_property.FindPropertyRelative("_shotSettings");

            DrawTitle(p_position, ShotTitle, ref currentY);
            DrawNestedProperty(
                p_position,
                shotSettings,
                "_fireInterval",
                ref currentY);
            DrawNestedProperty(
                p_position,
                shotSettings,
                "_trajectoryCount",
                ref currentY);
            DrawNestedProperty(
                p_position,
                shotSettings,
                "_spreadAngle",
                ref currentY);

            SerializedProperty fireResponseSettings =
                p_property.FindPropertyRelative("_fireResponseSettings");

            DrawTitle(p_position, FireResponseTitle, ref currentY);
            DrawNestedProperty(
                p_position,
                fireResponseSettings,
                "_recoil",
                ref currentY);
            DrawNestedProperty(
                p_position,
                fireResponseSettings,
                "_moveSpeedMultiplier",
                ref currentY);
            DrawNestedProperty(
                p_position,
                fireResponseSettings,
                "_cameraShakeName",
                ref currentY);

            DrawTitle(p_position, ActionTitle, ref currentY);
            DrawProperty(
                p_position,
                p_property.FindPropertyRelative("_defaultTriggerMode"),
                ref currentY);
            DrawProperty(
                p_position,
                p_property.FindPropertyRelative("_aimView"),
                ref currentY);
            DrawProperty(
                p_position,
                p_property.FindPropertyRelative("_chargeSettings"),
                ref currentY);

            DrawTitle(p_position, ImpactTitle, ref currentY);
            DrawProperty(
                p_position,
                p_property.FindPropertyRelative("_impactSettings"),
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

            SerializedProperty shotSettings =
                p_property.FindPropertyRelative("_shotSettings");
            SerializedProperty fireResponseSettings =
                p_property.FindPropertyRelative("_fireResponseSettings");

            AddTitleHeight(ref height);
            AddPropertyHeight(
                p_property.FindPropertyRelative("_weaponType"),
                ref height);
            AddPropertyHeight(
                p_property.FindPropertyRelative("_baseDamage"),
                ref height);
            AddPropertyHeight(
                p_property.FindPropertyRelative("_maxDistance"),
                ref height);

            AddTitleHeight(ref height);
            AddNestedPropertyHeight(
                shotSettings,
                "_fireInterval",
                ref height);
            AddNestedPropertyHeight(
                shotSettings,
                "_trajectoryCount",
                ref height);
            AddNestedPropertyHeight(
                shotSettings,
                "_spreadAngle",
                ref height);

            AddTitleHeight(ref height);
            AddNestedPropertyHeight(
                fireResponseSettings,
                "_recoil",
                ref height);
            AddNestedPropertyHeight(
                fireResponseSettings,
                "_moveSpeedMultiplier",
                ref height);
            AddNestedPropertyHeight(
                fireResponseSettings,
                "_cameraShakeName",
                ref height);

            AddTitleHeight(ref height);
            AddPropertyHeight(
                p_property.FindPropertyRelative("_defaultTriggerMode"),
                ref height);
            AddPropertyHeight(
                p_property.FindPropertyRelative("_aimView"),
                ref height);
            AddPropertyHeight(
                p_property.FindPropertyRelative("_chargeSettings"),
                ref height);

            AddTitleHeight(ref height);
            AddPropertyHeight(
                p_property.FindPropertyRelative("_impactSettings"),
                ref height);

            return height;
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

        private static void DrawNestedProperty(
            Rect p_position,
            SerializedProperty p_parent,
            string p_propertyName,
            ref float p_currentY)
        {
            DrawProperty(
                p_position,
                p_parent?.FindPropertyRelative(p_propertyName),
                ref p_currentY);
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

        private static void AddNestedPropertyHeight(
            SerializedProperty p_parent,
            string p_propertyName,
            ref float p_height)
        {
            AddPropertyHeight(
                p_parent?.FindPropertyRelative(p_propertyName),
                ref p_height);
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
