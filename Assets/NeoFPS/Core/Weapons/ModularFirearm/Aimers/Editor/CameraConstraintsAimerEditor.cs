#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using NeoFPS.ModularFirearms;

namespace NeoFPSEditor.ModularFirearms
{
    [CustomEditor(typeof(CameraConstraintsAimer))]
    public class CameraConstraintsAimerEditor : OffsetBaseAimerEditor
    {
        protected override void InspectConcreteAimerProperties()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_RotationTransition"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ConstraintsPriority"));
        }

        protected override void InspectConcreteAimerTransitions() { }
    }
}

#endif