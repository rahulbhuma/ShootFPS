#if UNITY_EDITOR

#if UNITY_STANDALONE // Should other platforms use Json text files saved to disk?
#define SETTINGS_USES_JSON
#endif

using UnityEngine;
using UnityEditor;
using NeoFPS;
using NeoFPS.Constants;

namespace NeoFPSEditor
{
    [CustomEditor(typeof(SettingsContextBase), true)]
    public class SettingsContextEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUILayout.Space();

            EditorGUILayout.HelpBox("Any changes to these values will be ignored in favour of the .settings files that are created when you first play the game (including in the editor).\nPress the button below to delete the relevant .settings files and revert to the values in this asset.", MessageType.Warning);
            if (GUILayout.Button("Delete User Settings File"))
            {
                var sc = target as SettingsContextBase;
                sc.DeleteSaveFile();
            }

            NeoFpsEditorGUI.Separator();
            EditorGUILayout.Space();

            OnInspectorGUIInternal();

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void OnInspectorGUIInternal()
        {
            base.OnInspectorGUI();
        }
    }
}

#endif