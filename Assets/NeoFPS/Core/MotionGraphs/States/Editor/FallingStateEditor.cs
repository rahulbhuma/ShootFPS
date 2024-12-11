#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using NeoFPS.CharacterMotion.States;

namespace NeoFPSEditor.CharacterMotion.States
{
    [CustomEditor(typeof(FallingState))]
    [HelpURL("http://docs.neofps.com/manual/motiongraphref-mgs-fallingstate.html")]
    public class FallingStateEditor : MotionGraphStateEditor
    {
        protected override void OnInspectorGUIInternal()
        {
            var root = container;

            EditorGUILayout.LabelField("Motion Data", EditorStyles.boldLabel);
            MotionGraphEditorGUI.FloatDataReferenceField(root, serializedObject.FindProperty("m_HorizontalAcceleration"));
            MotionGraphEditorGUI.FloatDataReferenceField(root, serializedObject.FindProperty("m_HorizontalDeceleration"));
            MotionGraphEditorGUI.FloatDataReferenceField(root, serializedObject.FindProperty("m_TopSpeed"));
            MotionGraphEditorGUI.FloatDataReferenceField(root, serializedObject.FindProperty("m_StrafeMultiplier"));
            MotionGraphEditorGUI.FloatDataReferenceField(root, serializedObject.FindProperty("m_ReverseMultiplier"));
            MotionGraphEditorGUI.FloatDataReferenceField(root, serializedObject.FindProperty("m_HorizontalDrag"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Falling Properties", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_MomentumConservation"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Damping"));
        }
    }
}

#endif