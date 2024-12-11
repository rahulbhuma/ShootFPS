#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using NeoFPS;
using System;

namespace NeoFPSEditor
{
    [CustomPropertyDrawer(typeof(FloatRangeLimitAttribute))]
    public class FloatRangeLimitAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Debug.Assert(property.type == "FloatRange", "Attempting to use FloatRangeLimit attribute with a property that is not an FloatRange");

            EditorGUI.PrefixLabel(position, label);

            position.x += EditorGUIUtility.labelWidth;
            position.width -= EditorGUIUtility.labelWidth;
            position.width = position.width / 2f - 4f;

            var min = property.FindPropertyRelative("min");
            var max = property.FindPropertyRelative("max");

            var limits = attribute as FloatRangeLimitAttribute;

            float minValue = min.floatValue;
            if (FloatRangeDrawer.RangeFloatField(position, "Min", ref minValue))
            {
                minValue = Mathf.Clamp(minValue, limits.limitMin, limits.limitMax);
                min.floatValue = minValue;
                if (max.floatValue < minValue)
                    max.floatValue = minValue;
            }

            position.x += position.width + 8f;

            float maxValue = max.floatValue;
            if (FloatRangeDrawer.RangeFloatField(position, "Max", ref maxValue))
            {
                maxValue = Mathf.Clamp(maxValue, limits.limitMin, limits.limitMax);
                max.floatValue = maxValue;
                if (min.floatValue > maxValue)
                    min.floatValue = maxValue;
            }
        }
    }
}

#endif
