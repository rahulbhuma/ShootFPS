#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.Behaviours;

namespace NeoFPSEditor.CharacterMotion.Behaviours
{
    [MotionGraphBehaviourEditor(typeof(SwimStrokeAudioBehaviour))]
    public class SwimStrokeAudioBehaviourEditor : MotionGraphBehaviourEditor
    {
        protected override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Clips"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Volume"));
        }
    }
}

#endif
