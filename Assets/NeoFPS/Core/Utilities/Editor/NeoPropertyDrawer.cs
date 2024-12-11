#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace NeoFPSEditor
{
    public abstract class NeoPropertyDrawer : PropertyDrawer
    {
        /// <summary>
        /// The padding for the contents of a GUI box
        /// </summary>
        public static readonly Vector2 boxPadding = new Vector2(2f * EditorGUIUtility.standardVerticalSpacing, EditorGUIUtility.standardVerticalSpacing);

        /// <summary>
        /// The height of a field/line in the inspector with padding
        /// </summary>
        public static readonly float fullLineHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        /// <summary>
        /// Squashes the provided rect down to a single line height
        /// </summary>
        /// <param name="position">The original rect (eg multiple fields)</param>
        /// <returns>A copy of the original rect squashed to the height of a single line</returns>
        public static Rect GetFirstLine (Rect position)
        {
            position.height = EditorGUIUtility.singleLineHeight;
            return position;
        }

        /// <summary>
        /// Returns a copy of the position rect shifted down 1 line
        /// </summary>
        /// <param name="position">The current line position rect</param>
        /// <returns>The shifted rect for the next line</returns>
        public static Rect NextLine(Rect position)
        {
            position.y += fullLineHeight;
            return position;
        }

        /// <summary>
        /// Returns a copy of the position rect shifted down based on its height and line spacing, so that the new rect sits exactly below the input
        /// </summary>
        /// <param name="position">The current position rect</param>
        /// <returns>The shifted rect</returns>
        public static Rect NextRect(Rect position)
        {
            position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
            return position;
        }

        /// <summary>
        /// Returns the GUI pixel height for the provided number of fields/lines in the inspector.
        /// </summary>
        /// <param name="numLines">The number of fields/lines</param>
        /// <returns>The height</returns>
        public static float GetHeight (int numLines)
        {
            return numLines * fullLineHeight;
        }

        /// <summary>
        /// Returns the GUI pixel height for the provided number of fields/lines in the inspector.
        /// </summary>
        /// <param name="numLines">The number of fields/lines</param>
        /// <param name="padding">An extra padding between lines</param>
        /// <returns>The height</returns>
        public static float GetHeight(int numLines, float padding)
        {
            return numLines * (fullLineHeight + padding);
        }

        /// <summary>
        /// Returns the GUI pixel height for a reorderable list with the provided contents.
        /// </summary>
        /// <param name="numEntries">The number of entries in the list</param>
        /// <param name="entryHeight">The height of a single entry in the list</param>
        /// <param name="hasControls">Does the list have +/- controls visible underneath</param>
        /// <returns>The height</returns>
        public static float GetListHeight(int numEntries, float entryHeight, bool hasControls)
        {
            float height = numEntries * (entryHeight + 2f);

            // Add a single line for "list is empty"
            if (numEntries == 0)
                height += fullLineHeight;

            // Add header
            height += fullLineHeight;

            // Add controls
            if (hasControls)
                height += 32;
            else
                height += 12;

            return height;
        }

        /// <summary>
        /// Returns the GUI pixel height of an array (not reorderable list) with the provided contents.
        /// </summary>
        /// <param name="numEntries">The number of entries in the array</param>
        /// <param name="entryHeight">The height of an individual entry</param>
        /// <returns>The height</returns>
        public static float GetArrayHeight(int numEntries, float entryHeight, bool isExpanded)
        {
            if (isExpanded)
            {
                float height = numEntries * (entryHeight + 2) + fullLineHeight * 2f;
                if (numEntries == 0)
                    height += 2;
                return height;
            }
            else
                return fullLineHeight;
        }


        /// <summary>
        /// Sets the editor GUI indent level and modifies the provided position rect.
        /// For example, if indent has increased, the rect will be shifted right and its width shrunk.
        /// If the indent has decreased, the rect will be shifted left and its width grown.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="indentLevel"></param>
        /// <returns></returns>
        public static Rect SetIndent(Rect position, int indentLevel)
        {
            EditorGUI.indentLevel = indentLevel;
            return EditorGUI.IndentedRect(position);
        }

        /// <summary>
        /// Increments the editor GUI indent level and returns an indented version of the source rect
        /// </summary>
        /// <param name="position">The unindented rect</param>
        /// <returns>The indented rect</returns>
        public static Rect Indent(Rect position)
        {
            ++EditorGUI.indentLevel;
            return EditorGUI.IndentedRect(position);
        }

        /// <summary>
        /// Decrements the editor GUI indent level and returns an indented version of the source rect
        /// </summary>
        /// <param name="position">The unindented rect</param>
        /// <returns>The indented rect</returns>
        public static Rect Unindent(Rect position)
        {
            --EditorGUI.indentLevel;
            return EditorGUI.IndentedRect(position);
        }

        /// <summary>
        /// Used to apply modified properties and abort the inspector UI to prevent layout vs draw mismatches
        /// </summary>
        /// <param name="property">The drawer's target property</param>
        public static void OnLayoutPropertyChanged(SerializedProperty property)
        {
            property.serializedObject.ApplyModifiedProperties();
            throw new ExitGUIException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static Rect DrawBackgroundBox(Rect position)
        {
            var boxRect = position;
            boxRect.x -= 16;
            boxRect.width += 16;

            EditorGUI.LabelField(boxRect, GUIContent.none, EditorStyles.helpBox);

            position.y += boxPadding.y;
            position.height -= 2f * boxPadding.y;
            position.width -= boxPadding.x;

            return position;
        }
    }
}

#endif