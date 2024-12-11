using NeoSaveGames;
using NeoSaveGames.Serialization;
using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS.ModularFirearms
{
    [HelpURL("https://docs.neofps.com/manual/weaponsref-mb-targetingsystemtracker.html")]
    public class TargetingSystemTracker : MonoBehaviour, ITargetTracker, IGuidedProjectileTargetTracker, INeoSerializableComponent
    {
        public event UnityAction<ITargetTracker> onDestroyed;

        private TargetType m_TargetType = TargetType.None;
        private Vector3 m_TargetVector = Vector3.zero;
        private Collider m_TargetCollider = null;
        private Transform m_TargetTransform = null;
        private bool m_WorldOffset = false;

        public enum TargetType
        {
            None,
            Vector,
            Collider,
            Transform,
            TransformWithOffset
        }

        public bool hasTarget
        {
            get { return m_TargetType != TargetType.None; }
        }

        public void SetTargetPosition(Vector3 target)
        {
            m_TargetVector = target;
            m_TargetType = TargetType.Vector;
        }

        public void SetTargetCollider(Collider target)
        {
            m_TargetCollider = target;
            m_TargetType = TargetType.Collider;
        }

        public void SetTargetTransform(Transform target)
        {
            m_TargetTransform = target;
            m_TargetType = TargetType.Transform;
        }

        public void SetTargetTransform(Transform target, Vector3 offset, bool worldOffset)
        {
            m_TargetTransform = target;
            m_TargetVector = offset;
            m_WorldOffset = worldOffset;
            m_TargetType = TargetType.TransformWithOffset;
        }

        public void ClearTarget()
        {
            m_TargetType = TargetType.None;
            m_TargetCollider = null;
            m_TargetTransform = null;
        }

        protected void OnDisable()
        {
            ClearTarget();
            if (onDestroyed != null)
                onDestroyed(this);
        }

        public bool GetTargetPosition(out Vector3 targetPosition)
        {
            switch (m_TargetType)
            {
                case TargetType.Vector:
                    targetPosition = m_TargetVector;
                    return true;
                case TargetType.Collider:
                    if (m_TargetCollider != null)
                    {
                        targetPosition = m_TargetCollider.bounds.center;
                        return true;
                    }
                    else
                    {
                        targetPosition = Vector3.zero;
                        return false;
                    }
                case TargetType.Transform:
                    if (m_TargetTransform != null)
                    {
                        Debug.Log("Target transform");
                        targetPosition = m_TargetTransform.position;
                        return true;
                    }
                    else
                    {
                        Debug.Log("Target transform failed");
                        targetPosition = Vector3.zero;
                        return false;
                    }
                case TargetType.TransformWithOffset:
                    if (m_TargetTransform != null)
                    {
                        Vector3 pos = m_TargetTransform.position;
                        if (m_WorldOffset)
                            pos += m_TargetVector;
                        else
                            pos += m_TargetTransform.rotation * m_TargetVector;
                        targetPosition = pos;
                        return true;
                    }
                    else
                    {
                        targetPosition = Vector3.zero;
                        return false;
                    }
                default:
                    targetPosition = Vector3.zero;
                    return false;
            }
        }

        #region INeoSerializableComponent IMPLEMENTATION

        private static readonly NeoSerializationKey k_TargetTypeKey = new NeoSerializationKey("targetType");
        private static readonly NeoSerializationKey k_TargetVectorKey = new NeoSerializationKey("targetVector");
        private static readonly NeoSerializationKey k_TargetColliderKey = new NeoSerializationKey("targetCollider");
        private static readonly NeoSerializationKey k_TargetTransformKey = new NeoSerializationKey("targetTransform");
        private static readonly NeoSerializationKey k_WorldSpaceKey = new NeoSerializationKey("world");

        public void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            writer.WriteValue(k_TargetTypeKey, (int)m_TargetType);
            switch(m_TargetType)
            {
                case TargetType.Vector:
                    writer.WriteValue(k_TargetVectorKey, m_TargetVector);
                    break;
                case TargetType.Collider:
                    writer.WriteComponentReference(k_TargetColliderKey, m_TargetCollider, nsgo);
                    break;
                case TargetType.Transform:
                    writer.WriteTransformReference(k_TargetTransformKey, m_TargetTransform, nsgo);
                    break;
                case TargetType.TransformWithOffset:
                    writer.WriteTransformReference(k_TargetTransformKey, m_TargetTransform, nsgo);
                    writer.WriteValue(k_TargetVectorKey, m_TargetVector);
                    writer.WriteValue(k_WorldSpaceKey, m_WorldOffset);
                    break;
            }
        }

        public void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            if (reader.TryReadValue(k_TargetTypeKey, out int t, 0))
            {
                m_TargetType = (TargetType)t;
                reader.TryReadValue(k_TargetVectorKey, out m_TargetVector, Vector3.zero);
                reader.TryReadValue(k_WorldSpaceKey, out m_WorldOffset, false);
                reader.TryReadTransformReference(k_TargetTransformKey, out m_TargetTransform, nsgo);
                reader.TryReadComponentReference(k_TargetColliderKey, out m_TargetCollider, nsgo);
            }
        }

        #endregion
    }
}
