#if UNITY_EDITOR

using NeoFPS;
using UnityEditor;
using UnityEngine;

namespace NeoFPSEditor
{
    [CustomPropertyDrawer(typeof (FloatRange))]
    public class FloatRangeDrawer : PropertyDrawer
    {
        private static readonly GUIContent k_Padding = new GUIContent(" ");

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PrefixLabel(position, label);

            position.x += EditorGUIUtility.labelWidth;
            position.width -= EditorGUIUtility.labelWidth;
            position.width = position.width / 2f - 4f;

            var min = property.FindPropertyRelative("min");
            var max = property.FindPropertyRelative("max");

            float minValue = min.floatValue;
            if (RangeFloatField(position, "Min", ref minValue))
            {
                min.floatValue = minValue;
                if (max.floatValue < minValue)
                    max.floatValue = minValue;
            }

            position.x += position.width + 8f;

            float maxValue = max.floatValue;
            if (RangeFloatField(position, "Max", ref maxValue))
            {
                max.floatValue = maxValue;
                if (min.floatValue > maxValue)
                    min.floatValue = maxValue;
            }
        }

        public static bool RangeFloatField(Rect position, string label, ref float value)
        {
            float oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 28;

            GUI.Label(position, label);
            float newValue = EditorGUI.DelayedFloatField(position, k_Padding, value);

            EditorGUIUtility.labelWidth = oldWidth;

            if (value != newValue)
            {
                value = newValue;
                return true;
            }
            else
                return false;
        }
    }
}

#endif