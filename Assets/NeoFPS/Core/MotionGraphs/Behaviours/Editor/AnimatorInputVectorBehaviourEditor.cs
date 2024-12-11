#if UNITY_EDITOR

using System;
using UnityEngine;
using UnityEditor;
using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.Behaviours;
using NeoFPS.CharacterMotion.Parameters;

namespace NeoFPSEditor.CharacterMotion.Behaviours
{
    [MotionGraphBehaviourEditor(typeof(AnimatorInputVectorBehaviour))]
    public class AnimatorInputVectorBehaviourEditor : MotionGraphBehaviourEditor
    {
        protected override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ForwardParamName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_StrafeParamName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Damping"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ForwardMultiplier"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_BackwardMultiplier"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_StrafeMultiplier"));
        }
    }
}

#endif