using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoFPS.SinglePlayer;
using NeoSaveGames.Serialization;
using NeoSaveGames;

namespace NeoFPS.Samples
{
    [RequireComponent(typeof(FpsSoloGameMinimal))]
    public class JetpacksMissilesIntro : MonoBehaviour, INeoSerializableComponent
    {
        [SerializeField, Tooltip("Should the messages be shown prior to spawn.")]
        private bool m_ShowMessages = true;
        [SerializeField, Tooltip("The intro messages to show before spawning the character.")]
        private string[] m_IntroMessages = { };

        private FpsSoloGameMinimal m_GameMode = null;
        private int m_TargetStep = 0;

        void Awake()
        {
            m_GameMode = GetComponent<FpsSoloGameMinimal>();
            m_GameMode.spawnOnStart = !m_ShowMessages;
        }

        private IEnumerator Start()
        {
            int currentStep = -1;

            yield return null;

            if (m_ShowMessages)
            {
                while (currentStep < m_IntroMessages.Length)
                {
                    yield return null;
                    yield return null;

                    if (currentStep != m_TargetStep)
                    {
                        currentStep = m_TargetStep;

                        if (currentStep == m_IntroMessages.Length)
                        {
                            if (FpsSoloCharacter.localPlayerCharacter == null)
                                m_GameMode.Respawn(m_GameMode.player);
                        }
                        else
                        {
                            InfoPopup.ShowPopup(m_IntroMessages[currentStep], OnIntroOK);
                        }
                    }
                }

                m_ShowMessages = false;
            }
            else
            {
                yield return null;

                if (FpsSoloCharacter.localPlayerCharacter == null)
                    m_GameMode.Respawn(m_GameMode.player);
            }
        }

        void OnIntroOK()
        {
            ++m_TargetStep;
        }

        private static readonly NeoSerializationKey k_ShowKey = new NeoSerializationKey("show");

        public void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            writer.WriteValue(k_ShowKey, m_ShowMessages);
        }

        public void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            reader.TryReadValue(k_ShowKey, out m_ShowMessages, m_ShowMessages);
        }
    }
}