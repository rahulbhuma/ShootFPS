using NeoSaveGames;
using NeoSaveGames.Serialization;
using System;
using System.Collections;
using UnityEngine;

namespace NeoFPS.ModularFirearms
{
    [HelpURL("https://docs.neofps.com/manual/weaponsref-mb-laserpointeraimerswitch.html")]
    public class LaserPointerAimerSwitch : MonoBehaviour, ILaserPointerWatcher, INeoSerializableComponent
    {
        [SerializeField, Tooltip("The aimer module associated with the laser pointers. This should be disabled on start.")]
        private BaseAimerBehaviour m_LaserAimerModule = null;

        [SerializeField, Tooltip("The minimum accuracy of the firearm when the laser is switched on.")]
        private float m_LaserMinAccuracy = 0.85f;

        private ModularFirearm m_Firearm = null;
        private BaseAimerBehaviour m_OriginalAimer = null;

        protected void Awake()
        {
            m_Firearm = GetComponentInParent<ModularFirearm>();
        }

        protected void OnEnable()
        {
            if (m_Firearm == null)
                m_Firearm = GetComponentInParent<ModularFirearm>();
        }

        public void RegisterLaserPointer(ILaserPointer laserPointer)
        {
            if (m_Firearm != null)
            {
                laserPointer.onToggleOn += OnToggleLaserOn;
                laserPointer.onToggleOff += OnToggleLaserOff;
            }
        }

        public void UnregisterLaserPointer(ILaserPointer laserPointer)
        {
            if (m_Firearm != null)
            {
                laserPointer.onToggleOn -= OnToggleLaserOn;
                laserPointer.onToggleOff -= OnToggleLaserOff;
            }
        }

        private void OnToggleLaserOn()
        {
            if (m_LaserAimerModule != null)
            {
                var previous = m_Firearm.aimer as BaseAimerBehaviour;
                if (previous != m_LaserAimerModule)
                {
                    m_OriginalAimer = previous;
                    m_LaserAimerModule.Enable();
                    m_Firearm.onAimerChange += OnAimerChanged;
                }
                else
                    m_OriginalAimer = null;
            }

            m_Firearm.minAccuracy = m_LaserMinAccuracy;
        }

        private void OnToggleLaserOff()
        {
            if (m_OriginalAimer != null)
            {
                m_Firearm.onAimerChange -= OnAimerChanged;
                m_OriginalAimer.Enable();
            }

            m_Firearm.minAccuracy = 0f;
        }

        private void OnAimerChanged(IModularFirearm firearm, IAimer aimer)
        {
            var cast = aimer as BaseAimerBehaviour;
            if (cast != m_LaserAimerModule)
            {
                m_OriginalAimer = cast;
                m_Firearm.onAimerChange -= OnAimerChanged;
                m_LaserAimerModule.enabled = true;
                m_Firearm.onAimerChange += OnAimerChanged;
            }
        }

        static readonly NeoSerializationKey k_AimerKey = new NeoSerializationKey("aimer");

        public void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            if (m_OriginalAimer != null)
                writer.WriteComponentReference(k_AimerKey, m_OriginalAimer, nsgo);
        }

        public void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            BaseAimerBehaviour aimer;
            if (reader.TryReadComponentReference(k_AimerKey, out aimer, nsgo))
                m_OriginalAimer = aimer;
        }
    }
}
