#if UNITY_EDITOR

using System;
using UnityEngine;
using UnityEditor;
using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.Behaviours;
using NeoFPS.CharacterMotion.Parameters;

namespace NeoFPSEditor.CharacterMotion.Behaviours
{
    [MotionGraphBehaviourEditor(typeof(AlignWithVelocityBehaviour))]
    public class AlignWithVelocityBehaviourEditor : MotionGraphBehaviourEditor
    {
        protected override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_MinSpeed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_AngleRange"));
        }
    }
}

#endif