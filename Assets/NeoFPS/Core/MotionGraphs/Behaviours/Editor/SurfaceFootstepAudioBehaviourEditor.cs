#if UNITY_EDITOR

using System;
using UnityEngine;
using UnityEditor;
using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.Behaviours;
using NeoFPS.CharacterMotion.Parameters;

namespace NeoFPSEditor.CharacterMotion.Behaviours
{
    [MotionGraphBehaviourEditor(typeof(SurfaceFootstepAudioBehaviour))]
    public class SurfaceFootstepAudioBehaviourEditor : MotionGraphBehaviourEditor
    {
        protected override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_AudioData"));

            var prop = serializedObject.FindProperty("m_CastDirection");
            EditorGUILayout.PropertyField(prop);

            switch (prop.enumValueIndex)
            {
                case 0:
                    break;
                case 1:
                    break;
                case 2:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_CastVector"));
                    break;
                case 3:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_CastVector"));
                    break;
                case 8:
                    MotionGraphEditorGUI.ParameterDropdownField<TransformParameter>(
                        owner.container,
                        serializedObject.FindProperty("m_TransformParameter")
                        );
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_CastVector"));
                    break;
                default:
                    MotionGraphEditorGUI.ParameterDropdownField<VectorParameter>(
                        owner.container,
                        serializedObject.FindProperty("m_VectorParameter")
                        );
                    break;
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_MinimumSpeed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_MaxRayDistance"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Persistent"));
        }
    }
}

#endif
