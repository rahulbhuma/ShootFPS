#if UNITY_EDITOR

using System;
using UnityEngine;
using UnityEditor;
using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.Behaviours;
using NeoFPS.CharacterMotion.Parameters;

namespace NeoFPSEditor.CharacterMotion.Behaviours
{
    [MotionGraphBehaviourEditor(typeof(ConstrainCameraPitchBehaviour))]
    public class ConstrainCameraPitchBehaviourEditor : MotionGraphBehaviourEditor
    {
        protected override void OnInspectorGUI()
        {
            var minConstraint = serializedObject.FindProperty("m_MinPitchConstraint");
            var maxConstraint = serializedObject.FindProperty("m_MaxPitchConstraint");

            EditorGUILayout.PropertyField(minConstraint);
            switch (minConstraint.enumValueIndex)
            {
                case 0: // Value
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_MinimumPitch"));
                    break;
                case 1: // Parameter
                    MotionGraphEditorGUI.ParameterDropdownField<FloatParameter>(owner.container, serializedObject.FindProperty("m_MinPitchParameter"));
                    break;
                case 2: // Slope
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_MinGroundOffset"));
                    break;
            }

            EditorGUILayout.PropertyField(maxConstraint);
            switch (maxConstraint.enumValueIndex)
            {
                case 0: // Value
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_MaximumPitch"));
                    break;
                case 1: // Parameter
                    MotionGraphEditorGUI.ParameterDropdownField<FloatParameter>(owner.container, serializedObject.FindProperty("m_MaxPitchParameter"));
                    break;
                case 2: // Slope
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_MaxHorizonOffset"));
                    break;
            }
        }
    }
}

#endif
