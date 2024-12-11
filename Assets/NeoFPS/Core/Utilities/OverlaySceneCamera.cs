using UnityEngine;
using NeoFPS.SinglePlayer;
using System;

namespace NeoFPS.Samples
{
    [ExecuteAlways]
    public class OverlaySceneCamera : MonoBehaviour
    {
        [SerializeField, Tooltip("The height")]
        private Placement m_Placement = Placement.TopRight;
        [SerializeField, Tooltip("The height of the camera window as a multiplier for the screen height.")]
        private float m_NormalisedHeight = 0.4f;
        [SerializeField, Tooltip("The offset from the top of the screen as a multiplier of the screen height.")]
        private float m_NormalisedOffset = 0.025f;
        [SerializeField, Tooltip("The aspect ratio of the camera overlay (width/height).")]
        private float m_AspectRatio = 1f;

        enum Placement
        {
            TopLeft,
            TopRight
        }

        Rect GetCameraRect(Camera cam)
        {
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            float pixelHeight = m_NormalisedHeight * screenHeight;
            float pixelPadding = m_NormalisedOffset * screenHeight;
            float inverseWidth = 1f / screenWidth;

            if (m_Placement == Placement.TopLeft)
            {
                return new Rect(
                    pixelPadding * inverseWidth,
                    1f - m_NormalisedOffset - m_NormalisedHeight,
                    pixelHeight * m_AspectRatio * inverseWidth,
                    m_NormalisedHeight
                    );
            }
            else
            {
                return new Rect(
                    (screenWidth - pixelPadding - (pixelHeight * m_AspectRatio)) * inverseWidth,
                    1f - m_NormalisedOffset - m_NormalisedHeight,
                    pixelHeight * m_AspectRatio * inverseWidth,
                    m_NormalisedHeight
                    );
            }
        }

#if UNITY_EDITOR

        private Placement m_OldPlacement = Placement.TopRight;
        private float m_OldHeight = 0.4f;
        private float m_OldOffset = 0.4f;
        private float m_OldAspect = 0.4f;

        private void Update()
        {
            bool changed = false;

            if (m_Placement != m_OldPlacement)
            {
                m_OldPlacement = m_Placement;
                changed = true;
            }

            if (m_NormalisedHeight != m_OldHeight)
            {
                m_OldHeight = m_NormalisedHeight;
                changed = true;
            }

            if (m_NormalisedOffset != m_OldOffset)
            {
                m_OldOffset = m_NormalisedOffset;
                changed = true;
            }

            if (m_AspectRatio != m_OldAspect)
            {
                m_OldAspect = m_AspectRatio;
                changed = true;
            }

            if (changed)
            {
                // Get the camera rect
                var cam = GetComponentInChildren<Camera>();
                var oldRect = cam.rect;
                var newRect = GetCameraRect(cam);

                // Check if it's close
                bool matching = true;
                matching &= Almost(oldRect.x, newRect.x, 0.001f);
                matching &= Almost(oldRect.y, newRect.y, 0.001f);
                matching &= Almost(oldRect.width, newRect.width, 0.001f);
                matching &= Almost(oldRect.height, newRect.height, 0.001f);

                // Apply if not
                if (!matching)
                    cam.rect = newRect;
            }
        }

        bool Almost (float a, float b, float tolerance)
        {
            return Mathf.Abs(a - b) < tolerance;
        }

#else

        private void Start()
        {
            var cam = GetComponentInChildren<Camera>();
            cam.rect = GetCameraRect(cam);
        }

#endif
    }
}