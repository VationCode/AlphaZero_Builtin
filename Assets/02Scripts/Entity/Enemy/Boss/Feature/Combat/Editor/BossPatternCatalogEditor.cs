using UnityEditor;
using UnityEngine;

namespace Alpha.Boss.Editor
{
    // 중앙 패턴 설정과 Play 모드의 개별 실행을 제공한다.
    [CustomEditor(typeof(BossPatternCatalog)), CanEditMultipleObjects]
    public sealed class BossPatternCatalogEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (targets.Length != 1)
                return;
            BossPatternCatalog catalog = (BossPatternCatalog)target;
            BossPatternEditorGUI.DrawExecutionButtons(catalog.GetComponentInParent<BossCore>(),
                catalog.PatternCount, catalog.GetPattern, catalog);
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        private static void DrawPatternPreviews(BossPatternCatalog p_catalog, GizmoType p_type) =>
            BossPatternEditorGUI.DrawPatternPreviews(p_catalog.GetComponentInParent<BossCore>(true),
                p_catalog.transform, p_catalog.PatternCount, p_catalog.GetPattern);
    }
}
