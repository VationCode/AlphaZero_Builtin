using UnityEditor;
using UnityEngine;

namespace Alpha.Combat.Editor
{
    // 선택된 Hit Type의 넉백 값만 Impact Settings에 표시한다.
    [CustomPropertyDrawer(typeof(AttackImpactSettings))]
    public sealed class AttackImpactSettingsDrawer : PropertyDrawer
    {
        private const float VerticalSpacing = 2f;

        private static readonly GUIContent KnockbackDistanceLabel = new(
            "Knockback Distance",
            "이 공격이 피격자를 밀어낼 거리입니다.");

        private static readonly GUIContent KnockbackDurationLabel = new(
            "Knockback Duration",
            "설정한 넉백 거리까지 이동하는 시간입니다. 0이면 넉백하지 않습니다.");

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

            SerializedProperty hitType =
                p_property.FindPropertyRelative("_hitType");

            DrawProperty(
                p_position,
                hitType,
                null,
                ref currentY);

            if (!hitType.hasMultipleDifferentValues)
            {
                DrawSelectedKnockback(
                    p_position,
                    p_property,
                    (EHitType)hitType.enumValueIndex,
                    ref currentY);
            }

            DrawProperty(
                p_position,
                p_property.FindPropertyRelative("_recoveryDuration"),
                null,
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
            SerializedProperty hitType =
                p_property.FindPropertyRelative("_hitType");

            AddPropertyHeight(hitType, ref height);

            if (!hitType.hasMultipleDifferentValues)
            {
                AddSelectedKnockbackHeights(
                    p_property,
                    (EHitType)hitType.enumValueIndex,
                    ref height);
            }

            AddPropertyHeight(
                p_property.FindPropertyRelative("_recoveryDuration"),
                ref height);

            return height;
        }

        private static void DrawSelectedKnockback(
            Rect p_position,
            SerializedProperty p_property,
            EHitType p_hitType,
            ref float p_currentY)
        {
            SerializedProperty knockback =
                FindKnockbackProperty(p_property, p_hitType);

            if (knockback == null)
                return;

            DrawProperty(
                p_position,
                knockback.FindPropertyRelative("_distance"),
                KnockbackDistanceLabel,
                ref p_currentY);
            DrawProperty(
                p_position,
                knockback.FindPropertyRelative("_duration"),
                KnockbackDurationLabel,
                ref p_currentY);
        }

        private static void AddSelectedKnockbackHeights(
            SerializedProperty p_property,
            EHitType p_hitType,
            ref float p_height)
        {
            SerializedProperty knockback =
                FindKnockbackProperty(p_property, p_hitType);

            if (knockback == null)
                return;

            AddPropertyHeight(
                knockback.FindPropertyRelative("_distance"),
                ref p_height);
            AddPropertyHeight(
                knockback.FindPropertyRelative("_duration"),
                ref p_height);
        }

        private static SerializedProperty FindKnockbackProperty(
            SerializedProperty p_property,
            EHitType p_hitType)
        {
            string propertyName = p_hitType switch
            {
                EHitType.Light => "_lightKnockback",
                EHitType.Heavy => "_heavyKnockback",
                EHitType.Knockdown => "_knockdownKnockback",
                EHitType.Launch => "_launchKnockback",
                _ => null
            };

            return propertyName != null
                ? p_property.FindPropertyRelative(propertyName)
                : null;
        }

        private static void DrawProperty(
            Rect p_position,
            SerializedProperty p_property,
            GUIContent p_label,
            ref float p_currentY)
        {
            float height = EditorGUI.GetPropertyHeight(
                p_property,
                p_label,
                true);

            Rect propertyRect = new(
                p_position.x,
                p_currentY,
                p_position.width,
                height);

            if (p_label == null)
                EditorGUI.PropertyField(propertyRect, p_property, true);
            else
                EditorGUI.PropertyField(propertyRect, p_property, p_label, true);

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
