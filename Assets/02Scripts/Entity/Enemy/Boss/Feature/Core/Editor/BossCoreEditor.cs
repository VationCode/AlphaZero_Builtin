using UnityEditor;
using UnityEngine;

namespace Alpha.Boss.Editor
{
    [CustomEditor(typeof(BossCore)), CanEditMultipleObjects]
    public sealed class BossCoreEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (!Application.isPlaying || targets.Length != 1) return;
            BossCore boss = (BossCore)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(new GUIContent("State", "현재 보스 행동 상태입니다."), boss.Context.State.ToString());
                EditorGUILayout.TextField(new GUIContent("Selected Pattern", "공격 시작 거리까지 접근하며 유지하는 패턴입니다."), boss.SelectedPatternId ?? string.Empty);
                EditorGUILayout.ObjectField(new GUIContent("Target", "현재 감지한 Player입니다."), boss.Context.Target, typeof(Transform), true);
                EditorGUILayout.Toggle(new GUIContent("Is Moving", "추적이 수평 이동을 제어하고 있는지 표시합니다."), boss.Context.IsMoving);
                EditorGUILayout.FloatField(new GUIContent("Health", "현재 보스 체력입니다."), boss.HealthContext.CurrentHealth);
            }
            Repaint();
        }
    }
}
