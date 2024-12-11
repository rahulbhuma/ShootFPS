#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using NeoFPS;
using System;

namespace NeoFPSEditor
{
    [CustomPropertyDrawer(typeof(IntRangeLimitAttribute))]
    public class IntRangeLimitAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Debug.Assert(property.type == "IntRange", "Attempting to use IntRangeLimit attribute with a property that is not an IntRange");

            EditorGUI.PrefixLabel(position, label);

            position.x += EditorGUIUtility.labelWidth;
            position.width -= EditorGUIUtility.labelWidth;
            position.width = position.width / 2f - 4f;

            var min = property.FindPropertyRelative("min");
            var max = property.FindPropertyRelative("max");

            var limits = attribute as IntRangeLimitAttribute;

            int minValue = min.intValue;
            if (IntRangeDrawer.RangeIntField(position, "Min", ref minValue))
            {
                minValue = Mathf.Clamp(minValue, limits.limitMin, limits.limitMax);
                min.intValue = minValue;
                if (max.intValue < minValue)
                    max.intValue = minValue;
            }

            position.x += position.width + 8f;

            int maxValue = max.intValue;
            if (IntRangeDrawer.RangeIntField(position, "Max", ref maxValue))
            {
                maxValue = Mathf.Clamp(minValue, limits.limitMin, limits.limitMax);
                max.intValue = maxValue;
                if (min.intValue > maxValue)
                    min.intValue = maxValue;
            }
        }
    }
}

#endif
