#if UNITY_EDITOR

using NeoFPS;
using UnityEditor;
using UnityEngine;

namespace NeoFPSEditor
{
    [CustomPropertyDrawer(typeof (IntRange))]
    public class IntRangeDrawer : PropertyDrawer
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

            int minValue = min.intValue;
            if (RangeIntField(position, "Min", ref minValue))
            {
                min.intValue = minValue;
                if (max.intValue < minValue)
                    max.intValue = minValue;
            }

            position.x += position.width + 8f;

            int maxValue = max.intValue;
            if (RangeIntField(position, "Max", ref maxValue))
            {
                max.intValue = maxValue;
                if (min.intValue > maxValue)
                    min.intValue = maxValue;
            }
        }

        public static bool RangeIntField(Rect position, string label, ref int value)
        {
            float oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 28;

            GUI.Label(position, label);
            int newValue = EditorGUI.DelayedIntField(position, k_Padding, value);

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