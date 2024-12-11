#if UNITY_EDITOR

using UnityEditor;
using NeoFPS.ModularFirearms;

namespace NeoFPSEditor.ModularFirearms
{
    [CustomEditor(typeof(ParticleToRigibodyShellEject))]
    public class ParticleToRigibodyShellEjectEditor : BaseFirearmModuleBehaviourEditor
    {
        protected override void OnInspectorGUIInternal()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ParticleSystem"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_RigidbodyPrefab"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ShellScale"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_MaxParticles"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_SwapTime"));

            var delayTypeProp = serializedObject.FindProperty("m_DelayType");
            EditorGUILayout.PropertyField(delayTypeProp);
            if (delayTypeProp.enumValueIndex == 1)
            {
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Delay"));
                --EditorGUI.indentLevel;
            }
        }
    }
}

#endif