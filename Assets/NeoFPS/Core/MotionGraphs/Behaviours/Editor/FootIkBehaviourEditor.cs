#if UNITY_EDITOR

using System;
using UnityEngine;
using UnityEditor;
using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.Behaviours;
using NeoFPS.CharacterMotion.Parameters;

namespace NeoFPSEditor.CharacterMotion.Behaviours
{
    [MotionGraphBehaviourEditor(typeof(FootIkBehaviour))]
    public class FootIkBehaviourEditor : MotionGraphBehaviourEditor
    {
        GUIContent m_InTimeLabel = new GUIContent("In Time", "The time taken to blend in the IK effect");
        GUIContent m_OutTimeLabel = new GUIContent("Out Time", "The time taken to blend out of the IK effect");

        protected override void OnInspectorGUI()
        {
            var onEnterProp = serializedObject.FindProperty("m_OnEnter");
            var onExitProp = serializedObject.FindProperty("m_OnExit");

            EditorGUILayout.PropertyField(onEnterProp);
            switch (onEnterProp.enumValueIndex)
            {
                case 0:
                    ++EditorGUI.indentLevel;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_OnEnterTime"), m_InTimeLabel);
                    --EditorGUI.indentLevel;
                    break;
                case 1:
                    ++EditorGUI.indentLevel;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_OnEnterTime"), m_OutTimeLabel);
                    --EditorGUI.indentLevel;
                    break;
            }

            EditorGUILayout.PropertyField(onExitProp);
            switch (onEnterProp.enumValueIndex)
            {
                case 0:
                    ++EditorGUI.indentLevel;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_OnExitTime"), m_InTimeLabel);
                    --EditorGUI.indentLevel;
                    break;
                case 1:
                    ++EditorGUI.indentLevel;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_OnExitTime"), m_OutTimeLabel);
                    --EditorGUI.indentLevel;
                    break;
            }
        }
    }
}

#endif