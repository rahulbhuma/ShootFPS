#if UNITY_EDITOR && UNITY_POST_PROCESSING_STACK_V2

using NeoFPS;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace NeoFPSEditor
{
    [CustomEditor(typeof(PostProcessVolumeFix))]
    public class PostProcessVolumeFixEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var cast = target as PostProcessVolumeFix;
            var exists = cast.CheckVolumeExists();

            GUI.enabled = exists;
            if (GUILayout.Button("Pull Settings From Existing"))
                cast.CopySettingsFromExisting();

            GUI.enabled = !exists;
            if (GUILayout.Button("Add Post-Processing Volume"))
                cast.AddPostProcessVolume();

            GUI.enabled = true;
        }
    }
}

#endif