using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Alpha.Enemy.Editor
{
    // 거리 규칙 목록과 Pattern Settings를 참조하는 Dropdown을 표시한다.
    internal sealed class EnemyDistancePatternListDrawer
    {
        private const float ElementSpacing = 4f;
        private const float VerticalSpacing = 2f;
        private const float PreviewToggleWidth = 64f;

        private readonly SerializedProperty _distancePatterns;
        private readonly SerializedProperty _patterns;
        private readonly SerializedProperty _patternVisibility;
        private readonly ReorderableList _list;

        public EnemyDistancePatternListDrawer(
            SerializedObject p_serializedObject,
            SerializedProperty p_distancePatterns,
            SerializedProperty p_patterns,
            SerializedProperty p_patternVisibility)
        {
            _distancePatterns = p_distancePatterns;
            _patterns = p_patterns;
            _patternVisibility = p_patternVisibility;
            _list = new ReorderableList(
                p_serializedObject,
                _distancePatterns,
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
            EditorGUI.LabelField(p_rect, "Distance Patterns");
        }

        private float GetElementHeight(int p_index)
        {
            SerializedProperty element =
                _distancePatterns.GetArrayElementAtIndex(p_index);

            float height = EditorGUIUtility.singleLineHeight +
                           ElementSpacing;

            if (!element.isExpanded)
                return height;

            AddPropertyHeight(element, "_rangeName", ref height);
            AddPropertyHeight(element, "_minimumDistance", ref height);
            AddPropertyHeight(element, "_maximumDistance", ref height);
            height += VerticalSpacing +
                      EditorGUIUtility.singleLineHeight;
            AddPropertyHeight(element, "_selectionWeight", ref height);

            return height;
        }

        private void DrawElement(
            Rect p_rect,
            int p_index,
            bool p_isActive,
            bool p_isFocused)
        {
            SerializedProperty element =
                _distancePatterns.GetArrayElementAtIndex(p_index);

            p_rect.y += ElementSpacing * 0.5f;

            Rect foldoutRect = new(
                p_rect.x,
                p_rect.y,
                p_rect.width,
                EditorGUIUtility.singleLineHeight);

            Rect labelRect = foldoutRect;

            if (HasPatternVisibility)
            {
                labelRect.width -= PreviewToggleWidth + ElementSpacing;

                Rect toggleRect = new(
                    labelRect.xMax + ElementSpacing,
                    foldoutRect.y,
                    PreviewToggleWidth,
                    EditorGUIUtility.singleLineHeight);

                DrawPreviewToggle(toggleRect, p_index);
            }

            element.isExpanded = EditorGUI.Foldout(
                labelRect,
                element.isExpanded,
                CreateElementLabel(element, p_index),
                true);

            if (!element.isExpanded)
                return;

            float currentY = foldoutRect.yMax + VerticalSpacing;
            EditorGUI.indentLevel++;

            DrawProperty(p_rect, element, "_rangeName", ref currentY);
            DrawProperty(
                p_rect,
                element,
                "_minimumDistance",
                ref currentY);
            DrawProperty(
                p_rect,
                element,
                "_maximumDistance",
                ref currentY);
            DrawPatternPopup(p_rect, element, ref currentY);
            DrawProperty(
                p_rect,
                element,
                "_selectionWeight",
                ref currentY);

            EditorGUI.indentLevel--;
        }

        private void DrawPatternPopup(
            Rect p_position,
            SerializedProperty p_element,
            ref float p_currentY)
        {
            SerializedProperty patternIndex =
                p_element.FindPropertyRelative("_patternIndex");

            if (patternIndex == null)
                return;

            int patternCount = _patterns?.arraySize ?? 0;
            GUIContent[] options = new GUIContent[patternCount + 1];
            options[0] = new GUIContent("None");

            for (int index = 0; index < patternCount; index++)
            {
                SerializedProperty pattern =
                    _patterns.GetArrayElementAtIndex(index);
                SerializedProperty patternName =
                    pattern.FindPropertyRelative("_patternName");

                string displayName = patternName != null
                    ? patternName.stringValue
                    : string.Empty;

                options[index + 1] = new GUIContent(
                    string.IsNullOrWhiteSpace(displayName)
                        ? $"Pattern {index + 1}"
                        : displayName);
            }

            int selectedOption = patternIndex.intValue >= 0 &&
                                 patternIndex.intValue < patternCount
                ? patternIndex.intValue + 1
                : 0;

            Rect propertyRect = new(
                p_position.x,
                p_currentY,
                p_position.width,
                EditorGUIUtility.singleLineHeight);

            int nextOption = EditorGUI.Popup(
                propertyRect,
                new GUIContent("Pattern"),
                selectedOption,
                options);

            patternIndex.intValue = nextOption - 1;
            p_currentY += EditorGUIUtility.singleLineHeight +
                          VerticalSpacing;
        }

        private void DrawPreviewToggle(Rect p_rect, int p_index)
        {
            SerializedProperty visibility =
                _patternVisibility.GetArrayElementAtIndex(p_index);

            GUIContent content = new(
                "Preview",
                "Scene View에 이 Distance Pattern의 범위를 표시합니다.");

            visibility.boolValue = GUI.Toggle(
                p_rect,
                visibility.boolValue,
                content,
                EditorStyles.miniButton);
        }

        private GUIContent CreateElementLabel(
            SerializedProperty p_element,
            int p_index)
        {
            SerializedProperty rangeName =
                p_element.FindPropertyRelative("_rangeName");
            SerializedProperty minimumDistance =
                p_element.FindPropertyRelative("_minimumDistance");
            SerializedProperty maximumDistance =
                p_element.FindPropertyRelative("_maximumDistance");
            SerializedProperty patternIndex =
                p_element.FindPropertyRelative("_patternIndex");

            string name = rangeName != null &&
                          !string.IsNullOrWhiteSpace(rangeName.stringValue)
                ? rangeName.stringValue
                : $"Distance {p_index + 1}";

            string patternName = ResolvePatternName(
                patternIndex?.intValue ?? -1);

            float minimum = minimumDistance?.floatValue ?? 0f;
            float maximum = maximumDistance?.floatValue ?? 0f;

            return new GUIContent(
                $"{name} ({minimum:0.##} - {maximum:0.##}) → " +
                patternName);
        }

        private string ResolvePatternName(int p_patternIndex)
        {
            if (_patterns == null ||
                p_patternIndex < 0 ||
                p_patternIndex >= _patterns.arraySize)
            {
                return "None";
            }

            SerializedProperty pattern =
                _patterns.GetArrayElementAtIndex(p_patternIndex);
            SerializedProperty patternName =
                pattern.FindPropertyRelative("_patternName");

            return patternName != null &&
                   !string.IsNullOrWhiteSpace(patternName.stringValue)
                ? patternName.stringValue
                : $"Pattern {p_patternIndex + 1}";
        }

        private bool CanRemove(ReorderableList p_list)
        {
            return _distancePatterns.arraySize >
                   EnemyCombatModule.MinimumDistancePatternCount;
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
                _patternVisibility.DeleteArrayElementAtIndex(removeIndex);
            }

            ReorderableList.defaultBehaviours.DoRemoveButton(p_list);
            EnsureVisibilityCount();
        }

        private void Add(ReorderableList p_list)
        {
            int previousCount = _distancePatterns.arraySize;
            ReorderableList.defaultBehaviours.DoAddButton(p_list);

            if (_distancePatterns.arraySize > previousCount)
            {
                InitializeElement(_distancePatterns.arraySize - 1);
            }

            EnsureVisibilityCount();

            if (HasPatternVisibility &&
                _distancePatterns.arraySize > previousCount)
            {
                _patternVisibility
                    .GetArrayElementAtIndex(_distancePatterns.arraySize - 1)
                    .boolValue = true;
            }
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
            while (_distancePatterns.arraySize <
                   EnemyCombatModule.MinimumDistancePatternCount)
            {
                int newIndex = _distancePatterns.arraySize;
                _distancePatterns.InsertArrayElementAtIndex(newIndex);
                InitializeElement(newIndex);
            }
        }

        private void InitializeElement(int p_index)
        {
            if (p_index < 0 || p_index >= _distancePatterns.arraySize)
                return;

            SerializedProperty element =
                _distancePatterns.GetArrayElementAtIndex(p_index);

            element.FindPropertyRelative("_rangeName").stringValue =
                $"Distance {p_index + 1}";
            element.FindPropertyRelative("_minimumDistance").floatValue = 0f;
            element.FindPropertyRelative("_maximumDistance").floatValue = 2f;
            element.FindPropertyRelative("_patternIndex").intValue =
                _patterns != null && _patterns.arraySize > 0 ? 0 : -1;
            element.FindPropertyRelative("_selectionWeight").floatValue = 1f;
            element.isExpanded = true;
        }

        private void EnsureVisibilityCount()
        {
            if (!HasPatternVisibility)
                return;

            int previousCount = _patternVisibility.arraySize;
            _patternVisibility.arraySize = _distancePatterns.arraySize;

            for (int index = previousCount;
                 index < _patternVisibility.arraySize;
                 index++)
            {
                _patternVisibility
                    .GetArrayElementAtIndex(index)
                    .boolValue = true;
            }
        }

        private static void DrawProperty(
            Rect p_position,
            SerializedProperty p_parent,
            string p_relativeName,
            ref float p_currentY)
        {
            SerializedProperty property =
                p_parent.FindPropertyRelative(p_relativeName);

            if (property == null)
                return;

            float propertyHeight = EditorGUI.GetPropertyHeight(
                property,
                true);

            Rect propertyRect = new(
                p_position.x,
                p_currentY,
                p_position.width,
                propertyHeight);

            EditorGUI.PropertyField(propertyRect, property, true);
            p_currentY += propertyHeight + VerticalSpacing;
        }

        private static void AddPropertyHeight(
            SerializedProperty p_parent,
            string p_relativeName,
            ref float p_height)
        {
            SerializedProperty property =
                p_parent.FindPropertyRelative(p_relativeName);

            if (property == null)
                return;

            p_height += VerticalSpacing +
                        EditorGUI.GetPropertyHeight(property, true);
        }

        private bool HasPatternVisibility =>
            _patternVisibility != null &&
            _patternVisibility.isArray;
    }
}
