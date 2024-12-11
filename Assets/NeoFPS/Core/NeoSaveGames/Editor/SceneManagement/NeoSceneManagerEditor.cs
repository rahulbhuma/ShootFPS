#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeoSaveGames.SceneManagement
{
    [CustomEditor(typeof(NeoSceneManager))]
    public class NeoSceneManagerEditor : Editor
    {
        private const int k_SceneOK = 0;
        private const int k_SceneNotSet = 1;
        private const int k_SceneNotBuilt = 2;

        public bool CheckIsValid()
        {
            var loadingScene = serializedObject.FindProperty("m_DefaultLoadingScreenIndex");
            var menuScene = serializedObject.FindProperty("m_DefaultMainMenuIndex");
            return (loadingScene.intValue != -1 && menuScene.intValue != -1);                
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUILayout.HelpBox("Loading screens are scenes which are loaded before loading the target scene asynchronously, and unloaded once the target scene loading is complete.\n\nThe default loading screen and main menu scene are stored by scene build index. If you modify the build settings, make sure to check the default scenes below are still correct.", MessageType.Info);
            EditorGUILayout.Space();

            var menuSceneProperty = serializedObject.FindProperty("m_DefaultMainMenuIndex");
            var loadingSceneProperty = serializedObject.FindProperty("m_DefaultLoadingScreenIndex");
            var buildScenes = EditorBuildSettings.scenes;

            SceneAsset menuSceneObject = null;
            SceneAsset loadingSceneObject = null;

            if (menuSceneProperty.intValue > -1)
            {
                if (menuSceneProperty.intValue >= buildScenes.Length)
                {
                    menuSceneProperty.intValue = -1;
                    Debug.LogError("Default loading scene index was out of bounds for build settings, setting to -1.");
                }
                else
                    menuSceneObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(buildScenes[menuSceneProperty.intValue].path);
            }

            if (loadingSceneProperty.intValue > -1)
            {
                if (loadingSceneProperty.intValue >= buildScenes.Length)
                {
                    loadingSceneProperty.intValue = -1;
                    Debug.LogError("Default loading scene index was out of bounds for build settings, setting to -1.");
                }
                else
                    loadingSceneObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(buildScenes[loadingSceneProperty.intValue].path);
            }

            // Show the scene fields
            var newMenuSceneObj = EditorGUILayout.ObjectField("Default Main Menu Scene", menuSceneObject, typeof(SceneAsset), false);
            var newLoadingSceneObj = EditorGUILayout.ObjectField("Default Loading Screen", loadingSceneObject, typeof(SceneAsset), false);

            // Get main menu index from new scene
            if (newMenuSceneObj != menuSceneObject)
            {
                if (newMenuSceneObj == null)
                    menuSceneProperty.intValue = -1;
                else
                {
                    bool found = false;
                    var path = AssetDatabase.GetAssetPath(newMenuSceneObj);
                    for (int i = 0; i < buildScenes.Length; ++i)
                    {
                        if (buildScenes[i].path == path)
                        {
                            menuSceneProperty.intValue = i;
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                        Debug.LogError("Main menu scene must be added to build settings");
                }
            }

            // Get loading scene index from new scene
            if (newLoadingSceneObj != loadingSceneObject)
            {
                if (newLoadingSceneObj == null)
                    loadingSceneProperty.intValue = -1;
                else
                {
                    bool found = false;
                    var path = AssetDatabase.GetAssetPath(newLoadingSceneObj);
                    for (int i = 0; i < buildScenes.Length; ++i)
                    {
                        if (buildScenes[i].path == path)
                        {
                            loadingSceneProperty.intValue = i;
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                        Debug.LogError("Loading scene must be added to build settings");
                }
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_MinLoadScreenTime"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_OnSceneLoaded"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_OnSceneLoadFailed"));

            serializedObject.ApplyModifiedProperties();
        }

        /*
        int CheckScenePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return 1;

            var scenes = EditorBuildSettings.scenes;
            foreach (var s in scenes)
            {
                if (s.path == path)
                    return 0;
            }

            return 2;
        }
        */
    }
}

#endif
