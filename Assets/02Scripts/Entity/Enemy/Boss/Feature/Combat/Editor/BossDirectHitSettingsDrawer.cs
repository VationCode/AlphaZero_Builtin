using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Alpha.Boss.Editor
{
    // 직접 영역 판정 설정만 표시한다. 공통 피해 정보는 부모 공격 설정에서 편집한다.
    [CustomPropertyDrawer(typeof(BossDirectHitSettings))]
    public sealed class BossDirectHitSettingsDrawer : PropertyDrawer
    {
        private static readonly GUIContent[] MeleeTimings =
            { new("Automatic"), new("Animation Window") };
        private static readonly int[] MeleeTimingValues =
            { (int)EBossDamageTiming.Automatic, (int)EBossDamageTiming.AnimationWindow };

        public override void OnGUI(Rect p_position, SerializedProperty p_property, GUIContent p_label)
        {
            EditorGUI.BeginProperty(p_position, p_label, p_property);
            try
            {
                Rect row = new(p_position.x, p_position.y, p_position.width, EditorGUIUtility.singleLineHeight);
                p_property.isExpanded = EditorGUI.Foldout(row, p_property.isExpanded, p_label, true);
                if (!p_property.isExpanded)
                    return;
                using (new EditorGUI.IndentLevelScope())
                    foreach (string name in Fields(p_property))
                    {
                        SerializedProperty child = p_property.FindPropertyRelative(name);
                        row.y = row.yMax + EditorGUIUtility.standardVerticalSpacing;
                        row.height = EditorGUI.GetPropertyHeight(child, true);
                        if (name == "_timing" && AttackType(p_property) == EBossAttackType.Melee)
                        {
                            EditorGUI.BeginProperty(row, new GUIContent("Timing", child.tooltip), child);
                            bool mixed = EditorGUI.showMixedValue;
                            EditorGUI.showMixedValue = child.hasMultipleDifferentValues;
                            EditorGUI.BeginChangeCheck();
                            int timing = EditorGUI.IntPopup(row, new GUIContent("Timing", child.tooltip),
                                child.intValue, MeleeTimings, MeleeTimingValues);
                            if (EditorGUI.EndChangeCheck())
                                child.intValue = timing;
                            EditorGUI.showMixedValue = mixed;
                            EditorGUI.EndProperty();
                        }
                        else
                            EditorGUI.PropertyField(row, child, true);
                    }
            }
            finally { EditorGUI.EndProperty(); }
        }

        public override float GetPropertyHeight(SerializedProperty p_property, GUIContent p_label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (p_property.isExpanded)
                foreach (string name in Fields(p_property))
                    height += EditorGUIUtility.standardVerticalSpacing +
                        EditorGUI.GetPropertyHeight(p_property.FindPropertyRelative(name), true);
            return height;
        }

        private static IEnumerable<string> Fields(SerializedProperty p_property)
        {
            EBossAttackType? type = AttackType(p_property);
            bool direct = type == EBossAttackType.Melee || type == EBossAttackType.Rush;
            if (direct)
                yield return "_enabled";
            if (!direct)
                yield break;
            yield return "_timing";
            SerializedProperty timing = p_property.FindPropertyRelative("_timing");
            if (!timing.hasMultipleDifferentValues &&
                (timing.intValue == (int)EBossDamageTiming.AnimationWindow ||
                 (type == EBossAttackType.Melee && timing.intValue == (int)EBossDamageTiming.Automatic)))
            {
                yield return "_startTimeSeconds";
                yield return "_endTimeSeconds";
            }
            yield return "_area";
        }

        private static EBossAttackType? AttackType(SerializedProperty p_property)
        {
            int separator = p_property.propertyPath.LastIndexOf('.');
            if (separator < 0)
                return null;
            SerializedProperty settings = p_property.serializedObject.FindProperty(
                p_property.propertyPath.Substring(0, separator));
            return settings?.propertyType == SerializedPropertyType.ManagedReference &&
                settings.managedReferenceValue is BossAttackSettings attack ? attack.AttackType : null;
        }
    }
}

