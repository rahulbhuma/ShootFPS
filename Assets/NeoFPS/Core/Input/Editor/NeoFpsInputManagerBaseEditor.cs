#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using NeoFPS;
using NeoFPSEditor.ScriptGeneration;
using System;

namespace NeoFPSEditor
{
    [CustomEditor(typeof(NeoFpsInputManagerBase))]
    public class NeoFpsInputManagerBaseEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            InspectEmbedded();
        }

        public void InspectEmbedded()
        {
            serializedObject.UpdateIfRequiredOrScript();

            InspectInternal();

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void InspectInternal()
        {
            InspectInputContexts();
        }

        #region INPUT CONTEXTS

        private ConstantsGenerator m_InputContextsGenerator = null;
        public ConstantsGenerator inputContextsGenerator
        {
            get
            {
                if (m_InputContextsGenerator == null)
                    m_InputContextsGenerator = new ConstantsGenerator(serializedObject, "m_InputContextInfo", "m_InputContextDirty", "m_InputContextError");
                return m_InputContextsGenerator;
            }
        }

        void InspectInputContexts()
        {
            EditorGUILayout.LabelField("Input Contexts", EditorStyles.boldLabel);

            if (inputContextsGenerator.DoLayoutGenerator())
            {
                var scriptSource = serializedObject.FindProperty("m_ScriptTemplate").objectReferenceValue as TextAsset;
                if (scriptSource == null)
                {
                    Debug.LogError("Attempting to generate constants script when no source files has been set");
                }
                else
                {
                    var folderObject = serializedObject.FindProperty("m_ScriptFolder");
                    inputContextsGenerator.GenerateConstants(folderObject, "FpsInputContext", scriptSource.text);
                }
            }
        }

        #endregion
    }
}

#endif