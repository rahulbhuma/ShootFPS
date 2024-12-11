using NeoSaveGames;
using NeoSaveGames.Serialization;
using System;
using UnityEngine;

namespace NeoFPS
{
    public abstract class BaseWieldableStanceManager : MonoBehaviour, INeoSerializableComponent
    {
        [SerializeField]
        private Stance[] m_Stances = { };

        private int m_CurrentStance = -1;
        private IPoseHandler m_PoseHandler = null;
        private Animator m_Animator = null;
        private int m_Blockers = 0;

        enum PositionBlend
        {
            Lerp,
            EaseIn,
            EaseOut,
            EaseInOut,
            SwingAcross,
            SwingUp,
            Spring,
            Bounce,
            Overshoot
        }

        enum RotationBlend
        {
            Lerp,
            Slerp,
            EaseIn,
            EaseOut,
            EaseInOut,
            Spring,
            Bounce,
            Overshoot
        }

        [Serializable]
        struct Stance
        {
#pragma warning disable 0649

#if UNITY_EDITOR
            public bool expanded;
#endif

			[Tooltip("The name of the stance.")]
            public string name;
			[Tooltip("An optional name of a bool parameter in the weapon's animator.")]
            public string animatorBoolKey;
			[Tooltip("The position to move the weapon to in this stance.")]
            public Vector3 position;
			[Tooltip("The rotation of the weapon in this stance.")]
            public Vector3 rotation;
			[Tooltip("The easing method for blending between the source position and stance position on entering the stance.")]
            public PositionBlend inPositionBlend;
			[Tooltip("The easing method for blending between the source rotation and stance rotation on entering the stance.")]
            public RotationBlend inRotationBlend;
			[Tooltip("The time taken to enter the stance.")]
            public float inTime;
			[Tooltip("The easing method for blending between the stance position and idle position on exiting the stance.")]
            public PositionBlend outPositionBlend;
			[Tooltip("The easing method for blending between the stance rotation and Idle rotation on exiting the stance.")]
            public RotationBlend outRotationBlend;
			[Tooltip("The time taken to exit the stance.")]
            public float outTime;

#pragma warning restore 0649

            public int animatorBoolHash
            {
                get;
                set;
            }

            public PoseInformation poseInfo
            {
                get;
                set;
            }

            public void OnValidate()
            {
                position.x = Mathf.Clamp(position.x, -1f, 1f);
                position.y = Mathf.Clamp(position.y, -1f, 1f);
                position.z = Mathf.Clamp(position.z, -1f, 1f);
                rotation.x = Mathf.Clamp(rotation.x, -90f, 90f);
                rotation.y = Mathf.Clamp(rotation.y, -90f, 90f);
                rotation.z = Mathf.Clamp(rotation.z, -90f, 90f);
                inTime = Mathf.Clamp(inTime, 0f, 10f);
                outTime = Mathf.Clamp(outTime, 0f, 10f);

                if (poseInfo != null)
                {
                    poseInfo.position = position;
                    poseInfo.rotation = Quaternion.Euler(rotation);
                    poseInfo.interpolatePositionIn = GetPositionInterpolationMethod(inPositionBlend);
                    poseInfo.interpolateRotationIn = GetRotationInterpolationMethod(inRotationBlend);
                    poseInfo.interpolatePositionOut = GetPositionInterpolationMethod(outPositionBlend);
                    poseInfo.interpolateRotationOut = GetRotationInterpolationMethod(outRotationBlend);
                }
            }

            public void Awake()
            {
                if (!string.IsNullOrEmpty(animatorBoolKey))
                    animatorBoolHash = Animator.StringToHash(animatorBoolKey);
                else
                    animatorBoolHash = -1;

                poseInfo = new PoseInformation(
                    position,
                    Quaternion.Euler(rotation),
                    GetPositionInterpolationMethod(inPositionBlend),
                    GetRotationInterpolationMethod(inRotationBlend),
                    GetPositionInterpolationMethod(outPositionBlend),
                    GetRotationInterpolationMethod(outRotationBlend)
                    );
            }

            public void TransitionIn(BaseWieldableStanceManager mgr, IPoseHandler poseHandler)
            {
                poseHandler.PushPose(poseInfo, mgr, inTime);
            }

            public void TransitionOut(BaseWieldableStanceManager mgr, IPoseHandler poseHandler)
            {
                poseHandler.PopPose(mgr, outTime);
            }
        }

        public string currentStance
        {
            get
            {
                if (m_CurrentStance == -1)
                    return string.Empty;
                else
                    return m_Stances[m_CurrentStance].name;
            }
        }

        public bool isBlocked
        {
            get { return m_Blockers > 0; }
        }

        protected virtual void OnValidate()
        {
            for (int i = 0; i < m_Stances.Length; ++i)
                m_Stances[i].OnValidate();
        }

        protected virtual void Awake()
        {
            m_Animator = GetComponentInChildren<Animator>();
            m_PoseHandler = GetComponent<IPoseHandler>();
            if (m_PoseHandler == null)
            {
                Debug.LogError("WieldableStanceManager requires a component that implements IPoseHandler to function");
                enabled = false;
            }

            for (int i = 0; i < m_Stances.Length; ++i)
                m_Stances[i].Awake();
        }

        protected virtual void OnDisable()
        {
            m_Blockers = 0;
            ResetStance();
        }

        protected void AddBlocker()
        {
            ++m_Blockers;
            if (m_Blockers == 1 && m_CurrentStance != -1)
                m_Stances[m_CurrentStance].TransitionOut(this, m_PoseHandler);
        }

        protected void RemoveBlocker()
        {
            --m_Blockers;
            switch (m_Blockers)
            {
                case 0:
                    if (m_CurrentStance != -1)
                        m_Stances[m_CurrentStance].TransitionIn(this, m_PoseHandler);
                    break;
                case -1:
                    m_Blockers = 0;
                    break;
            }
        }

        static VectorInterpolationMethod GetPositionInterpolationMethod(PositionBlend blend)
        {
            switch (blend)
            {
                case PositionBlend.EaseIn:
                    return PoseTransitions.PositionEaseInQuadratic;
                case PositionBlend.EaseOut:
                    return PoseTransitions.PositionEaseOutQuadratic;
                case PositionBlend.SwingAcross:
                    return PoseTransitions.PositionSwingAcross;
                case PositionBlend.SwingUp:
                    return PoseTransitions.PositionSwingUp;
                case PositionBlend.Spring:
                    return PoseTransitions.PositionSpringIn;
                case PositionBlend.Bounce:
                    return PoseTransitions.PositionBounceIn;
                case PositionBlend.Overshoot:
                    return PoseTransitions.PositionOvershootIn;
                default:
                    return PoseTransitions.PositionLerp;
            }
        }

        static QuaternionInterpolationMethod GetRotationInterpolationMethod(RotationBlend blend)
        {
            switch (blend)
            {
                case RotationBlend.EaseIn:
                    return PoseTransitions.RotationEaseInQuadratic;
                case RotationBlend.EaseOut:
                    return PoseTransitions.RotationEaseOutQuadratic;
                case RotationBlend.Slerp:
                    return PoseTransitions.RotationSlerp;
                case RotationBlend.Spring:
                    return PoseTransitions.RotationSpringIn;
                case RotationBlend.Bounce:
                    return PoseTransitions.RotationBounceIn;
                case RotationBlend.Overshoot:
                    return PoseTransitions.RotationOvershootIn;
                default:
                    return PoseTransitions.RotationLerp;
            }
        }

        public void SetStance(string stanceName)
        {
            if (enabled)
            {
                if (m_PoseHandler == null)
                    m_PoseHandler = GetComponent<IPoseHandler>();

                if (stanceName == string.Empty)
                {
                    if (m_CurrentStance != -1)
                    {
                        // Reset pose
                        if (!isBlocked && m_CurrentStance != -1)
                            m_Stances[m_CurrentStance].TransitionOut(this, m_PoseHandler);
                        // Reset animator bool parameter
                        if (m_Animator != null && m_Stances[m_CurrentStance].animatorBoolHash != -1)
                            m_Animator.SetBool(m_Stances[m_CurrentStance].animatorBoolHash, false);
                        // Set stance to idle
                        m_CurrentStance = -1;
                    }
                }
                else
                {
                    for (int i = 0; i < m_Stances.Length; ++i)
                    {
                        if (m_Stances[i].name == stanceName)
                        {
                            if (m_CurrentStance != i)
                            {
                                if (m_CurrentStance != -1)
                                    m_Stances[m_CurrentStance].TransitionOut(this, m_PoseHandler);

                                if (!isBlocked)
                                    m_Stances[i].TransitionIn(this, m_PoseHandler);

                                // Set animator bool parameter
                                if (m_Animator != null)
                                {
                                    // Reset old
                                    if (m_CurrentStance != -1 && m_Stances[m_CurrentStance].animatorBoolHash != -1)
                                        m_Animator.SetBool(m_Stances[m_CurrentStance].animatorBoolHash, false);
                                    // Set new
                                    if (m_Stances[i].animatorBoolHash != -1)
                                        m_Animator.SetBool(m_Stances[i].animatorBoolHash, true);
                                }

                                m_CurrentStance = i;
                            }
                            break;
                        }
                    }
                }
            }
        }

        public void ResetStance()
        {
            if (m_CurrentStance != -1)
            {
                // Reset pose
                if (m_PoseHandler != null)
                    m_Stances[m_CurrentStance].TransitionOut(this, m_PoseHandler);
                // Reset animator bool parameter
                if (m_Animator != null && m_Stances[m_CurrentStance].animatorBoolHash != -1)
                    m_Animator.SetBool(m_Stances[m_CurrentStance].animatorBoolHash, false);
                // Set stance to idle
                m_CurrentStance = -1;
            }
        }

        private static readonly NeoSerializationKey k_CurrentStanceKey = new NeoSerializationKey("currentStance");
        private static readonly NeoSerializationKey k_PoseIdKey = new NeoSerializationKey("poseID");

        public void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            writer.WriteValue(k_CurrentStanceKey, m_CurrentStance);
        }

        public void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            reader.TryReadValue(k_CurrentStanceKey, out m_CurrentStance, -1);
        }
    }
}