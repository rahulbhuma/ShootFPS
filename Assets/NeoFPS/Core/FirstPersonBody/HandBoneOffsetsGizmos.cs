#if UNITY_EDITOR

using UnityEngine;

namespace NeoFPS
{
    public static class HandBoneOffsetsGizmos
    {
        private static Color[] m_FingerColors = null;
        private static Color m_HandColor = Color.white;
        private static Vector3[] m_FingerGizmoVertices = null;
        private static Vector3[] m_ArrowGizmoVertices = null;
        private static Vector3[] m_LeftGizmoVertices = null;
        private static Vector3[] m_RightOuterGizmoVertices = null;
        private static Vector3[] m_RightInnerGizmoVertices = null;

        delegate Transform BoneTransformDelegate(int index);

        public static void DrawHandGizmosHumanoid(Animator animator, HandBoneOffsets offsets, float fingerScale, float handScale)
        {
            DrawHandGizmosInternal(
                i => animator.GetBoneTransform((HumanBodyBones)(i + 24)),
                animator.GetBoneTransform(HumanBodyBones.LeftHand),
                animator.GetBoneTransform(HumanBodyBones.RightHand),
                offsets,
                fingerScale,
                handScale);
        }

        public static void DrawHandGizmosGeneric(Transform[] handBones, Transform leftHand, Transform rightHand, HandBoneOffsets offsets, float fingerScale, float handScale)
        {
            DrawHandGizmosInternal(
                i => GetFingerTransformGeneric(i, handBones),
                leftHand,
                rightHand,
                offsets,
                fingerScale,
                handScale);
        }

        static Transform[] s_Knuckles = new Transform[5];

        static void DrawHandGizmosInternal(BoneTransformDelegate getBoneTransform, Transform leftHand, Transform rightHand, HandBoneOffsets offsets, float fingerScale, float handScale)
        {
            InitialiseGizmoData();

            if (s_Knuckles == null)
                s_Knuckles = new Transform[5];

            // Draw the left hand
            if (leftHand != null)
                DrawHandGizmo(getBoneTransform, leftHand.position, leftHand.rotation * offsets.leftHandRotationOffset, true, handScale);

            // Draw the right hand
            if (rightHand != null)
                DrawHandGizmo(getBoneTransform, rightHand.position, rightHand.rotation * offsets.rightHandRotationOffset, false, handScale);

            // Iterate through finger bones
            for (int i = 0; i < 30; ++i)
            {
                // Get the transform
                var boneTransform = getBoneTransform(i);
                if (boneTransform != null)
                {
                    // Get the offset
                    var offset = GetFingerRotationOffset(i, offsets);

                    // Get the next bone transform
                    Transform nextBone = null;
                    if (i % 3 == 2) // Check if finger tip
                    {
                        if (boneTransform.childCount > 0)
                            nextBone = boneTransform.GetChild(0);
                    }
                    else
                        nextBone = getBoneTransform(i + 1);

                    // Get the bone length
                    float boneLength = 0.025f;
                    if (nextBone != null)
                        boneLength = Mathf.Max(nextBone.localPosition.magnitude, 0.02f);

                    // Draw the finger bone
                    DrawFingerGizmo(
                        boneTransform.position,
                        boneTransform.rotation * offset,
                        boneLength,
                        fingerScale,
                        m_FingerColors[(i / 3) % m_FingerColors.Length]
                        );
                }
            }

            Gizmos.color = Color.white;
        }

        static Transform GetFingerTransformGeneric(int index, Transform[] fingerBones)
        {
            if (index >= 0 && index < fingerBones.Length) // 30 = 10 fingers * 3 joints
                return fingerBones[index];
            else
                return null;
        }

        static Quaternion GetFingerRotationOffset(int index, HandBoneOffsets offsets)
        {
            bool offset = index < 15 ? offsets.offsetLeftFingers : offsets.offsetRightFingers;
            if (offset)
                return offsets.GetFingerRotation((HumanBodyBones)(index + 24));
            else
                return Quaternion.identity;
        }

        static Vector3 GetFingerBoneVertex(int index, float boneLength, float boneWidth)
        {
            var result = m_FingerGizmoVertices[index] * boneWidth;
            if (index > (m_FingerGizmoVertices.Length - 1) / 2)
                result.z += boneLength;
            return result;
        }

        static void DrawFingerGizmo(Vector3 startPosition, Quaternion startRotation, float boneLength, float fingerScale, Color color)
        {
            Gizmos.color = color;

            for (int j = 0; j < m_FingerGizmoVertices.Length; ++j)
            {
                int next = j + 1;
                if (next == m_FingerGizmoVertices.Length)
                    next = 0;


                Gizmos.DrawLine(
                    startPosition + startRotation * GetFingerBoneVertex(j, boneLength, fingerScale),
                    startPosition + startRotation * GetFingerBoneVertex(next, boneLength, fingerScale)
                    );
            }

            // Draw the finger bone origin
            Gizmos.DrawLine(
                startPosition + startRotation * GetFingerBoneVertex(0, boneLength, fingerScale),
                startPosition + startRotation * GetFingerBoneVertex(2, boneLength, fingerScale)
                );
        }

        static void DrawHandGizmo(BoneTransformDelegate getBoneTransform, Vector3 startPosition, Quaternion startRotation, bool left, float scale)
        {
            Gizmos.color = m_HandColor;

            int startIndex = left ? 0 : 15;
            s_Knuckles[0] = getBoneTransform(0 + startIndex);
            s_Knuckles[1] = getBoneTransform(3 + startIndex);
            s_Knuckles[2] = getBoneTransform(6 + startIndex);
            s_Knuckles[3] = getBoneTransform(9 + startIndex);
            s_Knuckles[4] = getBoneTransform(12 + startIndex);

            Vector3 handInnerStart = startRotation * new Vector3(scale * 0.5f, 0f, 0f);
            if (left)
            {
                DrawLines(m_LeftGizmoVertices, startPosition, startRotation, scale);
                DrawLines(m_ArrowGizmoVertices, startPosition, startRotation, scale);
            }
            else
            {
                handInnerStart *= -1f;
                DrawLines(m_RightInnerGizmoVertices, startPosition, startRotation, scale);
                DrawLines(m_RightOuterGizmoVertices, startPosition, startRotation, scale);
                DrawLines(m_ArrowGizmoVertices, startPosition, startRotation, scale);
            }

            // Connect knuckle to knuckle
            Gizmos.DrawLine(startPosition + handInnerStart, s_Knuckles[0].position);
            Gizmos.DrawLine(s_Knuckles[0].position, s_Knuckles[1].position);
            Gizmos.DrawLine(s_Knuckles[1].position, s_Knuckles[2].position);
            Gizmos.DrawLine(s_Knuckles[2].position, s_Knuckles[3].position);
            Gizmos.DrawLine(s_Knuckles[3].position, s_Knuckles[4].position);
            Gizmos.DrawLine(s_Knuckles[4].position, startPosition + -handInnerStart);
        }

        static void DrawLines(Vector3[] vertices, Vector3 position, Quaternion rotation, float scale)
        {
            int length = vertices.Length;
            for (int i = 0; i < length; ++i)
            {
                Gizmos.DrawLine(
                    position + rotation * vertices[i] * scale,
                    position + rotation * vertices[(i + 1) % length] * scale
                );
            }
        }

        static void InitialiseGizmoData()
        {
            if (m_FingerGizmoVertices == null)
            {
                m_FingerGizmoVertices = new Vector3[]
                {
                    new Vector3(-1f, 0f, 0f),
                    new Vector3(0f, 1f, 0f),
                    new Vector3(1f, 0f, 0f),
                    new Vector3(1f, 0f, -1f),
                    new Vector3(0f, 0f, 0f),
                    new Vector3(-1f, 0f, -1f)
                };
            }

            if (m_FingerColors == null)
            {
                float m_FadeToWhite = 0.3f;
                m_FingerColors = new Color[]
                {
                    new Color(1f, m_FadeToWhite, m_FadeToWhite),
                    new Color(1f, 1f, m_FadeToWhite),
                    new Color(m_FadeToWhite, 1f, m_FadeToWhite),
                    new Color(m_FadeToWhite, 1f, 1f),
                    new Color(1f, m_FadeToWhite, 1f)
                };
                m_HandColor = new Color(m_FadeToWhite, m_FadeToWhite, 1f);
            }

            if (m_ArrowGizmoVertices == null)
            {
                m_ArrowGizmoVertices = new Vector3[]
                {
                    new Vector3(-1f, 0f, 0f),
                    new Vector3(-1f, 0f, 2.5f),
                    new Vector3(-1.5f, 0f, 2.5f),
                    new Vector3(0f, 0f, 4f),
                    new Vector3(1.5f, 0f, 2.5f),
                    new Vector3(1f, 0f, 2.5f),
                    new Vector3(1f, 0f, 0f),
                    new Vector3(-1f, 0f, 0f),
                    new Vector3(0f, 1f, 0f),
                    new Vector3(1f, 0f, 0f),
                };
                for (int i = 0; i < m_ArrowGizmoVertices.Length; ++i)
                    m_ArrowGizmoVertices[i] = m_ArrowGizmoVertices[i] * 0.5f;
            }

            if (m_LeftGizmoVertices == null)
            {
                m_LeftGizmoVertices = new Vector3[]
                {
                    new Vector3(0f, 0f, 0f),
                    new Vector3(3f, 0f, 0f),
                    new Vector3(3f, 0f, 1f),
                    new Vector3(1f, 0f, 1f),
                    new Vector3(1f, 0f, 5f),
                    new Vector3(0f, 0f, 5f)
                };
                for (int i = 0; i < m_LeftGizmoVertices.Length; ++i)
                    m_LeftGizmoVertices[i] = (m_LeftGizmoVertices[i] + new Vector3(-1.5f, 0f, 1f)) / 5f;
            }

            if (m_RightOuterGizmoVertices == null)
            {
                m_RightOuterGizmoVertices = new Vector3[]
                {
                    new Vector3(0f, 0f, 0f),
                    new Vector3(1f, 0f, 0f),
                    new Vector3(1f, 0f, 2f),
                    new Vector3(2f, 0f, 2f),
                    new Vector3(2f, 0f, 0f),
                    new Vector3(3f, 0f, 0f),
                    new Vector3(3f, 0f, 2f),
                    new Vector3(2.5f, 0f, 2.5f),
                    new Vector3(3f, 0f, 3f),
                    new Vector3(3f, 0f, 4f),
                    new Vector3(2f, 0f, 5f),
                    new Vector3(0f, 0f, 5f)
                };
                for (int i = 0; i < m_RightOuterGizmoVertices.Length; ++i)
                    m_RightOuterGizmoVertices[i] = (m_RightOuterGizmoVertices[i] + new Vector3(-1.5f, 0f, 1f)) / 5f;
            }

            if (m_RightInnerGizmoVertices == null)
            {
                m_RightInnerGizmoVertices = new Vector3[]
                {
                    new Vector3(1f, 0f, 3f),
                    new Vector3(2f, 0f, 3f),
                    new Vector3(2f, 0f, 4f),
                    new Vector3(1f, 0f, 4f)
                };
                for (int i = 0; i < m_RightInnerGizmoVertices.Length; ++i)
                    m_RightInnerGizmoVertices[i] = (m_RightInnerGizmoVertices[i] + new Vector3(-1.5f, 0f, 1f)) / 5f;
            }
        }
    }
}

#endif