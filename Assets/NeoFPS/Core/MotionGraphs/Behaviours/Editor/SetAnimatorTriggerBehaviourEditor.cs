#if UNITY_EDITOR

using System;
using UnityEngine;
using UnityEditor;
using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.Behaviours;
using NeoFPS.CharacterMotion.Parameters;

namespace NeoFPSEditor.CharacterMotion.Behaviours
{
    [MotionGraphBehaviourEditor(typeof(SetAnimatorTriggerBehaviour))]
    public class SetAnimatorTriggerBehaviourEditor : MotionGraphBehaviourEditor
    {
        protected override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ParameterName"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_OnEnter"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_OnExit"));
        }
    }
}

#endif
