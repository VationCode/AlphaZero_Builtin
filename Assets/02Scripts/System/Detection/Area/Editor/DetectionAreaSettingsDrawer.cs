using UnityEditor;
using UnityEngine;

namespace Alpha.Detection.Editor
{
    // 선택한 탐지 형태에 필요한 설정만 중첩 Inspector에 표시한다.
    [CustomPropertyDrawer(typeof(DetectionAreaSettings), true)]
    public sealed class DetectionAreaSettingsDrawer : PropertyDrawer
    {
        private const float VerticalSpacing = 2f;

        public override void OnGUI(
            Rect p_position,
            SerializedProperty p_property,
            GUIContent p_label)
        {
            EditorGUI.BeginProperty(
                p_position,
                p_label,
                p_property);

            Rect lineRect = CreateLineRect(p_position, 0);
            p_property.isExpanded = EditorGUI.Foldout(
                lineRect,
                p_property.isExpanded,
                p_label,
                true);

            if (!p_property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            SerializedProperty shape =
                p_property.FindPropertyRelative("_shape");

            int lineIndex = 1;
            EditorGUI.indentLevel++;

            DrawProperty(
                p_position,
                shape,
                new GUIContent("Detection Type"),
                ref lineIndex);

            DrawProperty(
                p_position,
                p_property.FindPropertyRelative("_localOffset"),
                null,
                ref lineIndex);

            DrawProperty(
                p_position,
                p_property.FindPropertyRelative("_yawOffset"),
                new GUIContent(
                    "Yaw Offset",
                    "공격자 정면을 기준으로 판정 영역을 회전합니다."),
                ref lineIndex);

            if (!shape.hasMultipleDifferentValues)
            {
                EDetectionAreaShape selectedShape =
                    (EDetectionAreaShape)shape.enumValueIndex;

                switch (selectedShape)
                {
                    case EDetectionAreaShape.ForwardBox:
                        DrawProperty(
                            p_position,
                            p_property.FindPropertyRelative("_width"),
                            null,
                            ref lineIndex);
                        DrawProperty(
                            p_position,
                            p_property.FindPropertyRelative("_length"),
                            null,
                            ref lineIndex);
                        break;

                    case EDetectionAreaShape.ForwardSector:
                        DrawProperty(
                            p_position,
                            p_property.FindPropertyRelative("_radius"),
                            null,
                            ref lineIndex);
                        DrawProperty(
                            p_position,
                            p_property.FindPropertyRelative("_angle"),
                            null,
                            ref lineIndex);
                        break;

                    case EDetectionAreaShape.Radial:
                        DrawProperty(
                            p_position,
                            p_property.FindPropertyRelative("_radius"),
                            null,
                            ref lineIndex);
                        break;
                }
            }

            DrawProperty(
                p_position,
                p_property.FindPropertyRelative("_height"),
                null,
                ref lineIndex);
            DrawProperty(
                p_position,
                p_property.FindPropertyRelative("_targetMask"),
                null,
                ref lineIndex);
            DrawProperty(
                p_position,
                p_property.FindPropertyRelative("_triggerInteraction"),
                null,
                ref lineIndex);

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(
            SerializedProperty p_property,
            GUIContent p_label)
        {
            if (!p_property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            SerializedProperty shape =
                p_property.FindPropertyRelative("_shape");

            int childLineCount = 6;

            if (!shape.hasMultipleDifferentValues)
            {
                EDetectionAreaShape selectedShape =
                    (EDetectionAreaShape)shape.enumValueIndex;

                childLineCount += selectedShape ==
                                  EDetectionAreaShape.Radial
                    ? 1
                    : 2;
            }

            int totalLineCount = childLineCount + 1;

            return totalLineCount * EditorGUIUtility.singleLineHeight +
                   (totalLineCount - 1) * VerticalSpacing;
        }

        private static void DrawProperty(
            Rect p_position,
            SerializedProperty p_property,
            GUIContent p_label,
            ref int p_lineIndex)
        {
            Rect lineRect = CreateLineRect(
                p_position,
                p_lineIndex);

            if (p_label == null)
                EditorGUI.PropertyField(lineRect, p_property);
            else
                EditorGUI.PropertyField(lineRect, p_property, p_label);

            p_lineIndex++;
        }

        private static Rect CreateLineRect(
            Rect p_position,
            int p_lineIndex)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;

            return new Rect(
                p_position.x,
                p_position.y +
                p_lineIndex * (lineHeight + VerticalSpacing),
                p_position.width,
                lineHeight);
        }
    }
}
