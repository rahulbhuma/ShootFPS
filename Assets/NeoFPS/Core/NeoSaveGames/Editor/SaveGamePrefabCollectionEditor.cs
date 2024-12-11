#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using NeoSaveGames;
using NeoSaveGames.Serialization;

namespace NeoFPSEditor.SaveGames
{
    [CustomEditor(typeof(SaveGamePrefabCollection), true)]
    public class SaveGamePrefabCollectionEditor : SaveInfoBaseEditor
    {
        public sealed override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            InspectRecreatableItems(false, false);

            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif