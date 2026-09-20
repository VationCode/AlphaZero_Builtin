using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Alpha.Boss.Editor
{
    // Projectile과 GroundWave가 실제 사용하는 설정만 표시한다.
    [CustomPropertyDrawer(typeof(BossRangeAttackSettings))]
    public sealed class BossRangeAttackSettingsDrawer : PropertyDrawer
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

        internal static IEnumerable<SerializedProperty> VisibleProperties(SerializedProperty p_property)
        {
            SerializedProperty mode = p_property.FindPropertyRelative("_attackMode");
            yield return mode;
            yield return p_property.FindPropertyRelative("_directionType");
            yield return p_property.FindPropertyRelative("_maximumDistance");
            if (mode != null && !mode.hasMultipleDifferentValues)
            {
                if ((EBossRangeAttackMode)mode.intValue == EBossRangeAttackMode.Projectile)
                    yield return p_property.FindPropertyRelative("_projectilePrefab");
                else if ((EBossRangeAttackMode)mode.intValue == EBossRangeAttackMode.GroundWave)
                    yield return p_property.FindPropertyRelative("_groundWavePrefab");
            }
            yield return p_property.FindPropertyRelative("_fireGroups");
        }
    }
}
