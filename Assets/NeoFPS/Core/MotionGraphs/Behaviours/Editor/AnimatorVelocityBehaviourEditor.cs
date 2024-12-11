#if UNITY_EDITOR

using System;
using UnityEngine;
using UnityEditor;
using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.Behaviours;
using NeoFPS.CharacterMotion.Parameters;

namespace NeoFPSEditor.CharacterMotion.Behaviours
{
    [MotionGraphBehaviourEditor(typeof(AnimatorVelocityBehaviour))]
    public class AnimatorVelocityBehaviourEditor : MotionGraphBehaviourEditor
    {
        protected override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ForwardParamName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_StrafeParamName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_DampingTime"));
        }
    }
}

#endif