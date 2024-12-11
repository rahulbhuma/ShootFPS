#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using NeoSaveGames.Serialization;

namespace NeoFPSEditor.SaveGames
{
    [CustomEditor(typeof(NeoSerializedScene), true)]
    public class NeoSerializedSceneEditor : SaveInfoBaseEditor
    {
        public sealed override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            GUI.enabled = false;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            GUI.enabled = true;

            OnInspectorGUITop();
            InspectRecreatableItems(true, true);
            OnInspectorGUIBottom();

            serializedObject.ApplyModifiedProperties();
        }

        public virtual void OnInspectorGUITop()
        { }

        public virtual void OnInspectorGUIBottom()
        {
            var prop = serializedObject.FindProperty("m_Assets");
            while (prop.NextVisible(false))
                EditorGUILayout.PropertyField(prop, true);
        }
    }
}

#endif