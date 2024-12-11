using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS
{
    [ExecuteAlways]
    public class MapCaptureCamera : MonoBehaviour
    {
        [SerializeField, Tooltip("The width of the output texture in pixels")]
        private int m_TextureWidth = 1024;
        [SerializeField, Tooltip("The height of the output texture in pixels")]
        private int m_TextureHeight = 1024;
        [SerializeField, Tooltip("The size in meters of the capture volume (along the height axis of the texture)")]
        private float m_WorldSize = 100f;

        private void OnValidate()
        {
            m_TextureWidth = Mathf.Clamp(m_TextureWidth, 64, 4096);
            m_TextureHeight = Mathf.Clamp(m_TextureHeight, 64, 4096);
            m_WorldSize = Mathf.Clamp(m_WorldSize, 10f, 1000f);
        }

        private void Awake()
        {
            if (Application.isPlaying)
                Destroy(gameObject);
        }

        public Texture2D Capture()
        {
            // Create camera object and position
            var camObject = new GameObject("Camera");
            var camTransform = camObject.transform;
            camTransform.SetParent(transform);
            camTransform.localPosition = Vector3.up * 100f;
            camTransform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            // Add camera
            var cam = camObject.AddComponent<Camera>();
            cam.cullingMask = new PhysicsFilter(
                PhysicsFilter.LayerFilter.Default |
                PhysicsFilter.LayerFilter.Water |
                PhysicsFilter.LayerFilter.EnvironmentDetail |
                PhysicsFilter.LayerFilter.MovingPlatforms
                );
            cam.orthographic = true;
            cam.farClipPlane = 200f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.orthographicSize = m_WorldSize * 0.5f;
            cam.allowHDR = false;

            // Render texture
            var rt = new RenderTexture(m_TextureWidth, m_TextureHeight, 32, RenderTextureFormat.ARGB32, 0);
            cam.targetTexture = rt;

            // Render
            cam.Render();

            // Convert to texture
            Texture2D tex = new Texture2D(m_TextureWidth, m_TextureHeight, TextureFormat.RGBA32, false);
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();

            // Reset
            RenderTexture.active = null;
            cam.targetTexture = null;
            cam.enabled = false;
            DestroyImmediate(rt);
            DestroyImmediate(camObject);

            return tex;
        }

        private void OnDrawGizmos()
        {
            Vector2 bounds = new Vector2(m_WorldSize, m_WorldSize);
            if (m_TextureWidth != m_TextureHeight)
                bounds.x *= m_TextureWidth / (float)m_TextureHeight;

            ExtendedGizmos.DrawBoxMarker2D(transform.position, transform.rotation, bounds, Color.red);
        }
    }
}