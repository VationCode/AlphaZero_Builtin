using UnityEditor;
using UnityEngine;

namespace Alpha.Combat.Editor
{
    // 공격별 수치 대신 공용 시스템에 등록된 설정 타입을 선택한다.
    [CustomPropertyDrawer(typeof(AttackImpactSettings))]
    public sealed class AttackImpactSettingsDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect p_position, SerializedProperty p_property, GUIContent p_label)
        {
            EditorGUI.BeginProperty(p_position, p_label, p_property);
            SerializedProperty type = p_property.FindPropertyRelative("_reactionType");
            HitReactionSystem system = HitReactionSystem.Shared;
            int count = system != null ? system.TypeCount : 0;
            string[] names = new string[count + 1];
            names[0] = string.IsNullOrEmpty(type.stringValue)
                ? "기존 설정 (공용 타입을 선택하세요)"
                : $"누락된 타입: {type.stringValue}";
            int selected = 0;
            for (int index = 0; index < count; index++)
            {
                names[index + 1] = system.GetTypeName(index);
                if (names[index + 1] == type.stringValue)
                    selected = index + 1;
            }

            Rect row = new(p_position.x, p_position.y, p_position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.showMixedValue = type.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            int next = EditorGUI.Popup(row, p_label.text, selected, names);
            if (EditorGUI.EndChangeCheck() && next > 0)
                type.stringValue = names[next];
            EditorGUI.showMixedValue = false;

            row.y += EditorGUIUtility.singleLineHeight + 2f;
            using (new EditorGUI.DisabledScope(system == null))
            {
                if (GUI.Button(EditorGUI.IndentedRect(row), "공용 넉백·넉다운 설정 열기"))
                {
                    Selection.activeObject = system;
                    EditorGUIUtility.PingObject(system);
                }
            }
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty p_property, GUIContent p_label) =>
            EditorGUIUtility.singleLineHeight * 2f + 2f;
    }
}