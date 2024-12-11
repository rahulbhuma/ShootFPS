#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using NeoFPS;
using System.IO;
using System;

namespace NeoFPSEditor
{
    [CustomEditor (typeof(MapCaptureCamera))]
    public class MapCaptureCameraEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Capture Image"))
            {
                // Get the captured texture
                var mapCamera = target as MapCaptureCamera;
                var texture = mapCamera.Capture();

                // Convert to PNG
                var png = texture.EncodeToPNG();

                // Get a unique path
                string path;
                int i = 0;
                do
                {
                    path = Path.GetFullPath(Application.dataPath + "/../output_" + i + ".png");
                }
                while (File.Exists(path));

                var f = File.Create(path);
                f.Write(png, 0, png.Length);
                f.Close();
                f.Dispose();

                Debug.Log("Wrote png image to " + path);
            }
        }
    }
}

#endif