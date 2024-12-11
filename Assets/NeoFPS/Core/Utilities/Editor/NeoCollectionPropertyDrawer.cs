#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace NeoFPSEditor
{
    public abstract class NeoCollectionPropertyDrawer : NeoPropertyDrawer
    {
        protected abstract SerializedProperty GetArrayProperty(SerializedProperty collectionProperty);
        protected abstract float GetElementSize(SerializedProperty elementProperty);
        protected abstract void ResetElement(SerializedProperty elementProperty);
        protected abstract void InspectElement(Rect position, SerializedProperty elementProperty);

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = GetHeight(2);
            var array = GetArrayProperty(property);
            for (int i = 0; i < array.arraySize; ++i)
            {
                var element = array.GetArrayElementAtIndex(i);
                height += GetHeight(1);
                if (element.isExpanded)
                    height += GetElementSize(array.GetArrayElementAtIndex(i)) + 2f * boxPadding.y;
            }
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var array = GetArrayProperty(property);

            position = GetFirstLine(position);
            if (GUI.Button(position, "Add Slot"))
            {
                array.InsertArrayElementAtIndex(0);
                ResetElement(array.GetArrayElementAtIndex(0));
            }

            position = NextLine(position);

            for (int i = 0; i < array.arraySize; ++i)
            {
                var element = array.GetArrayElementAtIndex(i);

                // Get padded rectangle
                float size = GetHeight(1);
                if (element.isExpanded)
                    size += GetElementSize(element);
                position.height = size + 2f * boxPadding.y;

                // Draw background rect
                Rect contents = DrawBackgroundBox(position);

                // Draw header
                contents = GetFirstLine(contents);
                element.isExpanded = EditorGUI.Foldout(contents, element.isExpanded, element.displayName, true);

                // Inspect contents
                if (element.isExpanded)
                {
                    contents = NextLine(contents);
                    contents.height = size;
                    InspectElement(contents, element);
                }

                position = NextRect(position);
            }

            position = GetFirstLine(position);
            if (GUI.Button(position, "Add Slot"))
            {
                int last = array.arraySize++;
                ResetElement(array.GetArrayElementAtIndex(last));
            }
        }    
    }
}

#endif