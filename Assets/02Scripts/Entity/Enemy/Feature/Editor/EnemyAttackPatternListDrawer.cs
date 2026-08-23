using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Alpha.Enemy.Editor
{
    // 공격 패턴 배열의 목록 표시와 추가·삭제 규칙을 담당한다.
    internal sealed class EnemyAttackPatternListDrawer
    {
        private const float ElementSpacing = 4f;

        private readonly SerializedProperty _patterns;
        private readonly ReorderableList _list;

        public EnemyAttackPatternListDrawer(
            SerializedObject p_serializedObject,
            SerializedProperty p_patterns)
        {
            _patterns = p_patterns;
            _list = new ReorderableList(
                p_serializedObject,
                _patterns,
                true,
                true,
                true,
                true);

            _list.drawHeaderCallback = DrawHeader;
            _list.elementHeightCallback = GetElementHeight;
            _list.drawElementCallback = DrawElement;
            _list.onCanRemoveCallback = CanRemove;
            _list.onRemoveCallback = Remove;
        }

        public void Draw()
        {
            EnsureMinimumCount();
            _list.DoLayoutList();
        }

        private static void DrawHeader(Rect p_rect)
        {
            EditorGUI.LabelField(p_rect, "Attack Patterns");
        }

        private float GetElementHeight(int p_index)
        {
            return EditorGUI.GetPropertyHeight(
                       _patterns.GetArrayElementAtIndex(p_index),
                       true) +
                   ElementSpacing;
        }

        private void DrawElement(
            Rect p_rect,
            int p_index,
            bool p_isActive,
            bool p_isFocused)
        {
            SerializedProperty element =
                _patterns.GetArrayElementAtIndex(p_index);

            p_rect.y += ElementSpacing * 0.5f;
            p_rect.height = EditorGUI.GetPropertyHeight(
                element,
                true);

            EditorGUI.PropertyField(
                p_rect,
                element,
                new GUIContent($"Pattern {p_index + 1}"),
                true);
        }

        private bool CanRemove(ReorderableList p_list)
        {
            return _patterns.arraySize >
                   EnemyCombatModule.MinimumPatternCount;
        }

        private void Remove(ReorderableList p_list)
        {
            if (!CanRemove(p_list))
                return;

            ReorderableList.defaultBehaviours.DoRemoveButton(p_list);
        }

        private void EnsureMinimumCount()
        {
            while (_patterns.arraySize <
                   EnemyCombatModule.MinimumPatternCount)
            {
                _patterns.InsertArrayElementAtIndex(
                    _patterns.arraySize);
            }
        }
    }
}
