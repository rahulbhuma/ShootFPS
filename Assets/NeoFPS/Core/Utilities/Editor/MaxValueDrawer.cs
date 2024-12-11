#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using NeoFPS;

namespace NeoFPSEditor
{
    [CustomPropertyDrawer(typeof(MaxValueAttribute))]
    public class MaxValueDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            if (property.propertyType == SerializedPropertyType.Integer)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.DelayedIntField(position, property, label);
                if (EditorGUI.EndChangeCheck())
                {
                    SerializedProperty maxValueProp = GetMaxValueProperty(property);
                    if (maxValueProp != null)
                        maxValueProp.intValue = property.intValue;
                }
            }
            else if (property.propertyType == SerializedPropertyType.Float)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.DelayedFloatField(position, property, label);
                if (EditorGUI.EndChangeCheck())
                {
                    SerializedProperty maxValueProp = GetMaxValueProperty(property);
                    if (maxValueProp != null)
                        maxValueProp.floatValue = property.floatValue;
                }
            }
            else
            {
                NeoFpsEditorGUI.MiniError("Requires int or float field");
            }

            EditorGUI.EndProperty();
        }

        SerializedProperty GetMaxValueProperty(SerializedProperty property)
        {
            // Get cast attribute
            var castAttribute = attribute as MaxValueAttribute;

            SerializedProperty maxValueProp = null;

            int split = property.propertyPath.LastIndexOf('.');
            if (split == -1)
            {
                maxValueProp = property.serializedObject.FindProperty(castAttribute.maxValueFieldName);
            }
            else
            {
                var path = property.propertyPath.Substring(0, split + 1) + castAttribute.maxValueFieldName;
                maxValueProp = property.serializedObject.FindProperty(path);
            }

            return maxValueProp;
        }
    }
}

#endif