#if UNITY_EDITOR && UNITY_POST_PROCESSING_STACK_V2

using NeoFPS;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace NeoFPSEditor
{
    [CustomEditor(typeof(PostProcessLayerFix))]
    public class PostProcessLayerFixEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var cast = target as PostProcessLayerFix;
            var exists = cast.CheckLayerExists();

            GUI.enabled = !exists;
            if (GUILayout.Button("Add Post-Processing Layer"))
                cast.AddPostProcessLayer();

            GUI.enabled = true;
        }
    }
}

#endif

