#if UNITY_EDITOR

using System;
using UnityEngine;
using UnityEditor;
using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.Behaviours;
using NeoFPS.CharacterMotion.Parameters;

namespace NeoFPSEditor.CharacterMotion.Behaviours
{
    [MotionGraphBehaviourEditor(typeof(SetAnimatorBoolBehaviour))]
    public class SetAnimatorBoolBehaviourEditor : MotionGraphBehaviourEditor
    {
        protected override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ParameterName"));

            var whenProp = serializedObject.FindProperty("m_When");
            EditorGUILayout.PropertyField(whenProp);

            switch (whenProp.enumValueIndex)
            {
                case 0:
                    InspectEnterProperties();
                    break;
                case 1:
                    InspectExitProperties();
                    break;
                case 2:
                    InspectEnterProperties();
                    InspectExitProperties();
                    break;
            }
        }

        void InspectEnterProperties()
        {
            var type = serializedObject.FindProperty("m_OnEnterType");
            EditorGUILayout.PropertyField(type);

            if (type.enumValueIndex == 0)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_OnEnterValue"));
            else
                MotionGraphEditorGUI.ParameterDropdownField<SwitchParameter>(owner.container, serializedObject.FindProperty("m_OnEnterParameter"));
        }

        void InspectExitProperties()
        {
            var type = serializedObject.FindProperty("m_OnExitType");
            EditorGUILayout.PropertyField(type);

            if (type.enumValueIndex == 0)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_OnExitValue"));
            else
                MotionGraphEditorGUI.ParameterDropdownField<SwitchParameter>(owner.container, serializedObject.FindProperty("m_OnExitParameter"));
        }
    }
}

#endif
