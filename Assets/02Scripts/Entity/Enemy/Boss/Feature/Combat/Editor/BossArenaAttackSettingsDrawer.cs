using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Alpha.Boss.Editor
{
    // Arena의 시간별 위치 그룹과 이동 설정을 표시한다.
    [CustomPropertyDrawer(typeof(BossArenaAttackSettings))]
    public sealed class BossArenaAttackSettingsDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect p_position, SerializedProperty p_property, GUIContent p_label)
        {
            EditorGUI.BeginProperty(p_position, p_label, p_property);
            try
            {
                Rect row = new(p_position.x, p_position.y, p_position.width, EditorGUIUtility.singleLineHeight);
                p_property.isExpanded = EditorGUI.Foldout(row, p_property.isExpanded, p_label, true);
                if (!p_property.isExpanded)
                    return;
                float y = row.yMax;
                using (new EditorGUI.IndentLevelScope())
                {
                    foreach (SerializedProperty child in VisibleProperties(p_property))
                    {
                        if (child == null)
                            continue;
                        y += EditorGUIUtility.standardVerticalSpacing;
                        float height = EditorGUI.GetPropertyHeight(child, true);
                        EditorGUI.PropertyField(new Rect(p_position.x, y, p_position.width, height), child, true);
                        y += height;
                    }
                }
            }
            finally { EditorGUI.EndProperty(); }
        }

        public override float GetPropertyHeight(SerializedProperty p_property, GUIContent p_label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (p_property.isExpanded)
                foreach (SerializedProperty child in VisibleProperties(p_property))
                    if (child != null)
                        height += EditorGUIUtility.standardVerticalSpacing + EditorGUI.GetPropertyHeight(child, true);
            return height;
        }

        private static IEnumerable<SerializedProperty> VisibleProperties(SerializedProperty p_property)
        {
            yield return p_property.FindPropertyRelative("_attackPrefab");
            yield return p_property.FindPropertyRelative("_moveSpeed");
            yield return p_property.FindPropertyRelative("_maximumDistance");
            yield return p_property.FindPropertyRelative("_spawnGroups");
        }
    }
}
