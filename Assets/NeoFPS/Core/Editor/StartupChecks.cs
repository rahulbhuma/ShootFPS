#if UNITY_EDITOR

using NeoFPSEditor.Hub;
using System;
using UnityEditor;

namespace NeoFPSEditor
{
    class StartupChecks
    {
        private const int k_FrameDelay = 5;
        private static readonly char[] k_DefinesSplitter = { ';' };

        static int s_FrameDelay = 0;
        static bool s_Processing = false;
        static int s_Index = 0;
        static Action[] s_Callbacks = new Action[]
        {
            CheckScriptingDefines,
            ShowHub,
            Completed
        };

        [InitializeOnLoadMethod]
        static void InitializeOnLoad()
        {
            // Skip if playing
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            else
            {
                s_Index = 0;
                s_FrameDelay = 0;
                s_Processing = false;
                EditorApplication.update += WaitForEditor;
            }
        }

        static void CheckScriptingDefines()
        {
            // Check scripting defines
#if UNITY_2021_2_OR_NEWER
            BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
            BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            var target = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(targetGroup);
            var defines = PlayerSettings.GetScriptingDefineSymbols(target);
#else
            var target = EditorUserBuildSettings.selectedBuildTargetGroup;
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
#endif
            var split = defines.Split(k_DefinesSplitter);
            if (Array.IndexOf(split, "NEOFPS") == -1)
            {
                defines += ";NEOFPS";
#if UNITY_2021_2_OR_NEWER
                PlayerSettings.SetScriptingDefineSymbols(target, defines);
#else
                PlayerSettings.SetScriptingDefineSymbolsForGroup(target, defines);
#endif
            }

            GetNextCallback();
        }

        static void WaitForEditor()
        {
            if (!s_Processing && !EditorApplication.isUpdating && !EditorApplication.isCompiling)
            {
                if (++s_FrameDelay > k_FrameDelay)
                {
                    s_Processing = true;
                    s_Callbacks[s_Index]();
                    s_FrameDelay = 0;
                }
            }
            else
                s_FrameDelay = 0;
        }

        static void GetNextCallback()
        {
            ++s_Index;
            s_Processing = false;
            s_FrameDelay = 0;
        }

        static void ShowHub()
        {
            NeoFpsHubEditor.ShowOnStartup(GetNextCallback);
        }

        static void Completed()
        {
            s_Index = 0;
            s_FrameDelay = 0;
            s_Processing = false;
            NeoFpsEditorPrefs.firstRun = false;
            EditorApplication.update -= WaitForEditor;
        }
    }
}

#endif
