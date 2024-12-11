#if UNITY_EDITOR

using System;
using UnityEngine;
using UnityEditor;
using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.Behaviours;
using NeoFPS.CharacterMotion.Parameters;

namespace NeoFPSEditor.CharacterMotion.Behaviours
{
    [MotionGraphBehaviourEditor(typeof(ConstrainCharacterHeadingBehaviour))]
    public class ConstrainCharacterHeadingBehaviourEditor : MotionGraphBehaviourEditor
    {
        private GUIContent m_TargetLabel = null;

        protected override void OnInspectorGUI()
        {
            var constraintType = serializedObject.FindProperty("m_ConstraintType");
            EditorGUILayout.PropertyField(constraintType);

            switch (constraintType.enumValueIndex)
            {
                case 0: // DirectionVector
                    {
                        MotionGraphEditorGUI.ParameterDropdownField<VectorParameter>(
                        owner.container,
                        serializedObject.FindProperty("m_Direction")
                        );
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_AngleRange"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Flipped"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Continuous"));
                    }
                    break;
                case 1: // TargetVector
                    {
                        if (m_TargetLabel == null)
                            m_TargetLabel = new GUIContent("Target", "The vector parameter containing the target point.");

                        MotionGraphEditorGUI.ParameterDropdownField<VectorParameter>(
                        owner.container,
                        serializedObject.FindProperty("m_Direction"),
                        m_TargetLabel
                        );
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_AngleRange"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Flipped"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Continuous"));
                    }
                    break;
                case 2: // TransformForward
                    {
                        MotionGraphEditorGUI.ParameterDropdownField<TransformParameter>(
                        owner.container,
                        serializedObject.FindProperty("m_Transform")
                        );
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_AngleRange"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Flipped"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Continuous"));
                    }
                    break;
                case 3: // TransformDirection
                    {
                        MotionGraphEditorGUI.ParameterDropdownField<TransformParameter>(
                        owner.container,
                        serializedObject.FindProperty("m_Transform")
                        );
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_AngleRange"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Flipped"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Continuous"));
                    }
                    break;
                case 4: // Velocity
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_AngleRange"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Flipped"));
                    }
                    break;
            }
        }
    }
}

#endif
