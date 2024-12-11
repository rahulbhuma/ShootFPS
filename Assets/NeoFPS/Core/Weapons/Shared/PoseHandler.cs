using NeoSaveGames.Serialization;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS
{
    public class PoseHandler : IPoseHandler
    {
        private List<InternalPoseInfo> m_Poses = null;
        private Transform m_PoseTransform = null;
        private NeoSerializedGameObject m_PathFromNSGO = null;
        private Vector3 m_SourcePosition = Vector3.zero;
        private Quaternion m_SourceRotation = Quaternion.identity;
        private VectorInterpolationMethod m_PositionBlend = Vector3.Lerp;
        private QuaternionInterpolationMethod m_RotationBlend = Quaternion.Lerp;
        private float m_PoseLerp = 1f;
        private float m_BlendMultiplier = 1f;
        //private int m_TopID = -1;

        private struct InternalPoseInfo
        {
            public PoseInformation poseInformation;
            public MonoBehaviour owner;
            public int priority;

            private static readonly NeoSerializationKey k_TargetPositionKey = new NeoSerializationKey("targetPosition");
            private static readonly NeoSerializationKey k_TargetRotationKey = new NeoSerializationKey("targetRotation");
            private static readonly NeoSerializationKey k_OwnerKey = new NeoSerializationKey("owner");
            private static readonly NeoSerializationKey k_PriorityKey = new NeoSerializationKey("priority");

            public InternalPoseInfo(PoseInformation poseInfo, MonoBehaviour o, int p)
            {
                poseInformation = poseInfo;
                owner = o;
                priority = p;
            }

            public void WriteProperties(INeoSerializer writer, int index, NeoSerializedGameObject pathFrom)
            {
                writer.PushContext(SerializationContext.ObjectUnformatted, index);
                writer.WriteValue(k_TargetPositionKey, poseInformation.position);
                writer.WriteValue(k_TargetRotationKey, poseInformation.rotation);
                writer.WriteValue(k_PriorityKey, priority);
                writer.WriteComponentReference(k_OwnerKey, owner, pathFrom);
                writer.PopContext(SerializationContext.ObjectUnformatted);
            }

            public static InternalPoseInfo ReadProperties(INeoDeserializer reader, int index, NeoSerializedGameObject pathFrom)
            {
                var result = new InternalPoseInfo();

                if (reader.PushContext(SerializationContext.ObjectUnformatted, index))
                {
                    // Read owner
                    MonoBehaviour owner;
                    if (reader.TryReadComponentReference(k_OwnerKey, out owner, pathFrom) && owner != null)
                    {
                        // Set owner
                        result.owner = owner;

                        // Read pose information & create
                        Vector3 position;
                        Quaternion rotation;
                        reader.TryReadValue(k_TargetPositionKey, out position, Vector3.zero);
                        reader.TryReadValue(k_TargetRotationKey, out rotation, Quaternion.identity);
                        result.poseInformation = new PoseInformation(position, rotation);

                        // Read priority
                        reader.TryReadValue(k_PriorityKey, out result.priority, 0);
                    }

                    reader.PopContext(SerializationContext.ObjectUnformatted, index);
                }

                return result;
            }
        }
        
        public PoseHandler(Transform t, NeoSerializedGameObject nsgo)
        {
            m_Poses = new List<InternalPoseInfo>(2);
            m_PoseTransform = t;
            m_PathFromNSGO = nsgo;
        }

        public void PushPose(PoseInformation pose, MonoBehaviour owner, float blendTime, int priority = 0)
        {
            if (pose == null || owner == null)
                return; // error

            // Get current last pose
            MonoBehaviour currentPoseOwner = null;
            if (m_Poses.Count > 0)
                currentPoseOwner = m_Poses[m_Poses.Count - 1].owner;

            // Search for existing pose and return id
            bool found = false;
            for (int i = m_Poses.Count - 1; i >= 0; --i)
            {
                if (m_Poses[i].owner == owner)
                {
                    // Update pose info
                    var p = m_Poses[i];
                    p.poseInformation = pose;
                    p.priority = priority;

                    // Reassign and sort
                    m_Poses[i] = p;

                    found = true;
                }
            }

            // If not found, add new entry
            if (!found)
            {
                m_Poses.Add(new InternalPoseInfo(pose, owner, priority));
            }

            // Sort (highest priority last)
            m_Poses.Sort(ComparePoseInfo);

            // Check if there's a new top
            MonoBehaviour top = m_Poses[m_Poses.Count - 1].owner;
            if (currentPoseOwner != top)
            {
                // Get interpolation methods
                var topPose = m_Poses[m_Poses.Count - 1].poseInformation;
                m_PositionBlend = topPose.interpolatePositionIn;
                m_RotationBlend = topPose.interpolateRotationIn;

                // Reset blend
                OnTopPoseChange(blendTime);
            }
        }

        public void PopPose(MonoBehaviour owner, float blendTime)
        {
            int topIndex = m_Poses.Count - 1;
            for (int i = topIndex; i >= 0; --i)
            {
                if (m_Poses[i].owner == owner)
                {

                    // Start the blend if required
                    if (i == topIndex)
                    {
                        // Get interpolation methods
                        var topPose = m_Poses[i].poseInformation;
                        m_PositionBlend = topPose.interpolatePositionOut;
                        m_RotationBlend = topPose.interpolateRotationOut;

                        // Remove the pose
                        m_Poses.RemoveAt(i);

                        OnTopPoseChange(blendTime);
                    }
                    else
                    {
                        // Remove the pose
                        m_Poses.RemoveAt(i);
                    }

                    break;
                }
            }
        }

        public PoseInformation GetPose(MonoBehaviour owner)
        {
            for (int i = m_Poses.Count - 1; i >= 0; --i)
            {
                if (m_Poses[i].owner == owner)
                    return m_Poses[i].poseInformation;
            }
            return null;
        }

        void OnTopPoseChange(float blendTime)
        {
            if (blendTime <= 0.001f)
            {
                // Set start position as target position
                if (m_Poses.Count == 0)
                {
                    m_SourcePosition = Vector3.zero;
                    m_SourceRotation = Quaternion.identity;
                }
                else
                {
                    var topPose = m_Poses[m_Poses.Count - 1].poseInformation;
                    m_SourcePosition = topPose.position;
                    m_SourceRotation = topPose.rotation;
                }

                // Move transform
                m_PoseTransform.localPosition = m_SourcePosition;
                m_PoseTransform.localRotation = m_SourceRotation;

                // Reset blend
                m_PoseLerp = 1f;
                m_BlendMultiplier = 0f;
            }
            else
            {
                // Set start position
                m_SourcePosition = m_PoseTransform.localPosition;
                m_SourceRotation = m_PoseTransform.localRotation;

                // Reset blend
                m_PoseLerp = 0f;
                m_BlendMultiplier = 1f / blendTime;
            }

        }

        int ComparePoseInfo(InternalPoseInfo lhs, InternalPoseInfo rhs)
        {
            return lhs.priority - rhs.priority;
        }

        public void UpdatePose()
        {
            if (m_PoseLerp < 1f)
            {
                // Update strength
                m_PoseLerp += Time.deltaTime * m_BlendMultiplier;
                if (m_PoseLerp > 1f)
                    m_PoseLerp = 1f;

                // Get target position & rotation
                Vector3 position = Vector3.zero;
                Quaternion rotation = Quaternion.identity;
                if (m_Poses.Count > 0)
                {
                    var topPose = m_Poses[m_Poses.Count - 1].poseInformation;
                    position = topPose.position;
                    rotation = topPose.rotation;
                }

                // Blend
                position = m_PositionBlend(m_SourcePosition, position, m_PoseLerp);
                rotation = m_RotationBlend(m_SourceRotation, rotation, m_PoseLerp);

                // Apply to transform
                m_PoseTransform.localPosition = position;
                m_PoseTransform.localRotation = rotation;
            }
            else
            {
                if (m_Poses.Count > 0)
                {
                    // Apply pose to transform
                    var topPose = m_Poses[m_Poses.Count - 1].poseInformation;
                    m_PoseTransform.localPosition = topPose.position;
                    m_PoseTransform.localRotation = topPose.rotation;
                }
            }
        }

        public void OnDisable()
        {
            //m_SourcePosition = m_TargetPosition;
            //m_SourceRotation = m_TargetRotation;
            //m_PoseTransform.localPosition = m_TargetPosition;
            //m_PoseTransform.localRotation = m_TargetRotation;
            //m_CustomPositionInterpolation = null;
            //m_CustomRotationInterpolation = null;
            //m_PoseLerp = 1f;

            // Iterate through and remove all negative poses?
        }

        private static readonly NeoSerializationKey k_PoseLerpKey = new NeoSerializationKey("poseLerp");
        private static readonly NeoSerializationKey k_SourcePositionKey = new NeoSerializationKey("sourcePos");
        private static readonly NeoSerializationKey k_SourceRotationKey = new NeoSerializationKey("sourceRot");
        private static readonly NeoSerializationKey k_InvDurationKey = new NeoSerializationKey("invDuration");
        private static readonly NeoSerializationKey k_NumPosesKey = new NeoSerializationKey("numPoses");

        public void WriteProperties(INeoSerializer writer)
        {
            // NB: Can't save/load delegates, so custom interpolation will be discarded

            // Write blend info
            if (m_PoseLerp < 1f)
            {
                writer.WriteValue(k_PoseLerpKey, m_PoseLerp);
                writer.WriteValue(k_SourcePositionKey, m_SourcePosition);
                writer.WriteValue(k_SourceRotationKey, m_SourceRotation);
                writer.WriteValue(k_InvDurationKey, m_BlendMultiplier);
            }

            // Write pose list
            writer.WriteValue(k_NumPosesKey, m_Poses.Count);
            for (int i = 0; i < m_Poses.Count; ++i)
                m_Poses[i].WriteProperties(writer, i, m_PathFromNSGO);
        }

        public void ReadProperties(INeoDeserializer reader)
        {
            // Read pose list
            int numPoses;
            if (reader.TryReadValue(k_NumPosesKey, out numPoses, 0) && numPoses > 0)
            {
                for (int i = 0; i < numPoses; ++i)
                {
                    var poseInfo = InternalPoseInfo.ReadProperties(reader, i, m_PathFromNSGO);
                    if (poseInfo.owner != null)
                        m_Poses.Add(poseInfo);
                }
            }

            // Read blend info
            if (reader.TryReadValue(k_PoseLerpKey, out m_PoseLerp, m_PoseLerp))
            {
                reader.TryReadValue(k_SourcePositionKey, out m_SourcePosition, m_SourcePosition);
                reader.TryReadValue(k_SourceRotationKey, out m_SourceRotation, m_SourceRotation);
                reader.TryReadValue(k_InvDurationKey, out m_BlendMultiplier, 50f);
            }
            //else
            //{
            //    m_PoseTransform.localPosition = Vector3.zero;
            //    m_PoseTransform.localRotation = Quaternion.identity;
            //}

            // Apply the pose ???
            UpdatePose();
        }
    }
}