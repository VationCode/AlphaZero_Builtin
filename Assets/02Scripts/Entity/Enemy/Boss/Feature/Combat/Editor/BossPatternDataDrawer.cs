using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Alpha.Boss.Editor
{
    // 공통 부모 설정을 먼저 표시하고 실제 자식 종류의 세부 설정만 표시한다.
    [CustomPropertyDrawer(typeof(BossPatternData))]
    public sealed class BossPatternDataDrawer : PropertyDrawer
    {
        private static readonly string[] CommonFields =
            { "_patternName", "_animationKey", "_selection", "_damageProfile" };
        private static readonly string[] PreviewFields =
            { "_showAttackRange", "_showMinimumDistance", "_showMaximumDistance" };
        private static readonly string[] AttackLabels =
            { "근접 타격", "원거리", "돌진 / 점프", "타겟 범위", "전장 범위" };
        private static readonly GUIContent[] AttackOptions =
            { new("근접 타격"), new("원거리"), new("돌진 / 점프"), new("타겟 범위"), new("전장 범위") };

        public override void OnGUI(Rect p_position, SerializedProperty p_property, GUIContent p_label)
        {
            EditorGUI.BeginProperty(p_position, p_label, p_property);
            try
            {
                SerializedProperty settings = p_property.FindPropertyRelative("_settings");
                Rect row = new(p_position.x, p_position.y, p_position.width, EditorGUIUtility.singleLineHeight);
                p_property.isExpanded = EditorGUI.Foldout(row, p_property.isExpanded, PatternLabel(settings, p_label), true);
                if (!p_property.isExpanded)
                    return;
                float y = row.yMax;
                using (new EditorGUI.IndentLevelScope())
                {
                    y += EditorGUIUtility.standardVerticalSpacing;
                    row.y = y;
                    BossAttackSettings current = settings.managedReferenceValue as BossAttackSettings;
                    EBossAttackType type = current?.AttackType ?? EBossAttackType.Melee;
                    using (new EditorGUI.DisabledScope(p_property.serializedObject.isEditingMultipleObjects))
                    {
                        EditorGUI.BeginChangeCheck();
                        int selected = EditorGUI.Popup(row, new GUIContent("공격 방식",
                            "같은 계열에서는 세부 설정을 유지합니다. 다른 계열로 변경하면 공통 설정만 유지하며 Undo로 복원할 수 있습니다."),
                            (int)type, AttackOptions);
                        if (EditorGUI.EndChangeCheck())
                            settings.managedReferenceValue = BossAttackSettings.ChangeType(current, (EBossAttackType)selected);
                    }
                    y = row.yMax;
                    foreach (SerializedProperty child in VisibleProperties(p_property, settings))
                    {
                        if (child == null)
                            continue;
                        y += EditorGUIUtility.standardVerticalSpacing;
                        float height = EditorGUI.GetPropertyHeight(child, true);
                        EditorGUI.BeginChangeCheck();
                        EditorGUI.PropertyField(new Rect(p_position.x, y, p_position.width, height), child, Label(child), true);
                        if (EditorGUI.EndChangeCheck())
                            SceneView.RepaintAll();
                        y += height;
                    }
                }
            }
            finally { EditorGUI.EndProperty(); }
        }

        public override float GetPropertyHeight(SerializedProperty p_property, GUIContent p_label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (!p_property.isExpanded)
                return height;
            height += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
            foreach (SerializedProperty child in VisibleProperties(p_property, p_property.FindPropertyRelative("_settings")))
                if (child != null)
                    height += EditorGUIUtility.standardVerticalSpacing + EditorGUI.GetPropertyHeight(child, true);
            return height;
        }

        private static IEnumerable<SerializedProperty> VisibleProperties(SerializedProperty p_pattern, SerializedProperty p_settings)
        {
            if (p_settings == null)
                yield break;
            foreach (string name in CommonFields)
                yield return p_settings.FindPropertyRelative(name);
            foreach (string name in PreviewFields)
                yield return p_pattern.FindPropertyRelative(name);
            if (!(p_settings.managedReferenceValue is BossAttackSettings settings))
                yield break;
            switch (settings.AttackType)
            {
                case EBossAttackType.Melee:
                    yield return p_settings.FindPropertyRelative("_directHit");
                    break;
                case EBossAttackType.Rush:
                    yield return p_settings.FindPropertyRelative("_directHit");
                    yield return p_settings.FindPropertyRelative("_movementAttack");
                    yield return p_settings.FindPropertyRelative("_rushAttack");
                    break;
                case EBossAttackType.Range:
                    foreach (SerializedProperty child in BossRangeAttackSettingsDrawer.VisibleProperties(p_settings))
                        yield return child;
                    break;
                case EBossAttackType.Area:
                    yield return p_settings.FindPropertyRelative("_areaAttack");
                    break;
                case EBossAttackType.Arena:
                    yield return p_settings.FindPropertyRelative("_arenaAttack");
                    break;
            }
        }

        private static GUIContent Label(SerializedProperty p_property)
        {
            string label = p_property.name switch
            {
                "_patternName" => "패턴 이름",
                "_animationKey" => "애니메이션",
                "_selection" => "사용 조건",
                "_damageProfile" => "공통 피해",
                "_directHit" => "직접 타격 판정",
                "_movementAttack" => "이동 공격",
                "_rushAttack" => "이동 공격 효과",
                "_areaAttack" => "타겟 범위 생성",
                "_arenaAttack" => "전장 범위 생성",
                "_showAttackRange" => "공격 범위 표시",
                "_showMinimumDistance" => "최소 실행 거리 표시",
                "_showMaximumDistance" => "최대 실행 거리 표시",
                _ => p_property.displayName
            };
            return new GUIContent(label, p_property.tooltip);
        }

        private static GUIContent PatternLabel(SerializedProperty p_settings, GUIContent p_fallback)
        {
            if (!(p_settings?.managedReferenceValue is BossAttackSettings settings))
                return p_fallback;
            string name = string.IsNullOrWhiteSpace(settings.PatternName) ? p_fallback.text : settings.PatternName;
            return new GUIContent($"{name} ({AttackLabels[(int)settings.AttackType]})");
        }
    }
}
