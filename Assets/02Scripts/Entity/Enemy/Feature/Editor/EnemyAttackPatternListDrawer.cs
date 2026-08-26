using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Alpha.Enemy.Editor
{
    // 공격 패턴 배열의 목록 표시와 추가·삭제 규칙을 담당한다.
    internal sealed class EnemyAttackPatternListDrawer
    {
        private const float ElementSpacing = 4f;
        private const float PreviewToggleWidth = 64f;

        private readonly SerializedProperty _patterns;
        private readonly SerializedProperty _patternVisibility;
        private readonly ReorderableList _list;

        public EnemyAttackPatternListDrawer(
            SerializedObject p_serializedObject,
            SerializedProperty p_patterns,
            SerializedProperty p_patternVisibility)
        {
            _patterns = p_patterns;
            _patternVisibility = p_patternVisibility;
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
            _list.onAddCallback = Add;
            _list.onReorderCallbackWithDetails = ReorderVisibility;
        }

        public void Draw()
        {
            EnsureMinimumCount();
            EnsureVisibilityCount();
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

            Rect propertyRect = p_rect;

            if (HasPatternVisibility)
            {
                propertyRect.width -=
                    PreviewToggleWidth + ElementSpacing;

                Rect toggleRect = new(
                    propertyRect.xMax + ElementSpacing,
                    p_rect.y,
                    PreviewToggleWidth,
                    EditorGUIUtility.singleLineHeight);

                DrawPreviewToggle(toggleRect, p_index);
            }

            EditorGUI.PropertyField(
                propertyRect,
                element,
                new GUIContent($"Pattern {p_index + 1}"),
                true);
        }

        private void DrawPreviewToggle(Rect p_rect, int p_index)
        {
            SerializedProperty visibility =
                _patternVisibility.GetArrayElementAtIndex(p_index);

            GUIContent content = new(
                "Preview",
                "Scene View에 이 Attack Pattern의 범위를 표시합니다.");

            visibility.boolValue = GUI.Toggle(
                p_rect,
                visibility.boolValue,
                content,
                EditorStyles.miniButton);
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

            int removeIndex = p_list.index;

            if (HasPatternVisibility &&
                removeIndex >= 0 &&
                removeIndex < _patternVisibility.arraySize)
            {
                _patternVisibility.DeleteArrayElementAtIndex(
                    removeIndex);
            }

            ReorderableList.defaultBehaviours.DoRemoveButton(p_list);
            EnsureVisibilityCount();
        }

        private void Add(ReorderableList p_list)
        {
            int previousCount = _patterns.arraySize;
            ReorderableList.defaultBehaviours.DoAddButton(p_list);
            EnsureVisibilityCount();

            if (!HasPatternVisibility ||
                _patterns.arraySize <= previousCount)
            {
                return;
            }

            _patternVisibility
                .GetArrayElementAtIndex(_patterns.arraySize - 1)
                .boolValue = true;
        }

        private void ReorderVisibility(
            ReorderableList p_list,
            int p_oldIndex,
            int p_newIndex)
        {
            if (!HasPatternVisibility ||
                p_oldIndex < 0 ||
                p_oldIndex >= _patternVisibility.arraySize ||
                p_newIndex < 0 ||
                p_newIndex >= _patternVisibility.arraySize)
            {
                return;
            }

            _patternVisibility.MoveArrayElement(
                p_oldIndex,
                p_newIndex);
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

        private void EnsureVisibilityCount()
        {
            if (!HasPatternVisibility)
                return;

            int previousCount = _patternVisibility.arraySize;
            _patternVisibility.arraySize = _patterns.arraySize;

            for (int index = previousCount;
                 index < _patternVisibility.arraySize;
                 index++)
            {
                _patternVisibility
                    .GetArrayElementAtIndex(index)
                    .boolValue = true;
            }
        }

        private bool HasPatternVisibility =>
            _patternVisibility != null &&
            _patternVisibility.isArray;
    }
}
