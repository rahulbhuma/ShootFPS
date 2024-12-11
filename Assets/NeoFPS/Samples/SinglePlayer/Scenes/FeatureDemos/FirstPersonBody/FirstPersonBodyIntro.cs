using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoFPS.SinglePlayer;
using NeoSaveGames.Serialization;
using NeoSaveGames;

namespace NeoFPS.Samples
{
    [RequireComponent(typeof(FpsSoloGameMinimal))]
    public class FirstPersonBodyIntro : MonoBehaviour
    {
        [SerializeField, Tooltip("Should the messages be shown prior to spawn.")]
        private bool m_ShowMessage = true;

#if UNITY_EDITOR
        private const string k_Message = "This demo requires third party animation assets.\n\nFor more information check the unity package and readme in the demo folder.";
#else
        private const string k_Message = "This demo uses third party animation assets (bought separately).";
#endif

        private FpsSoloGameMinimal m_GameMode = null;
        private bool m_Blocked = false;

        void Awake()
        {
            m_GameMode = GetComponent<FpsSoloGameMinimal>();
            m_GameMode.spawnOnStart = !m_ShowMessage;
        }

        private IEnumerator Start()
        {
            if (FpsSoloCharacter.localPlayerCharacter == null)
            {
                if (m_ShowMessage)
                {
                    m_Blocked = true;

                    yield return null;

                    InfoPopup.ShowPopup(k_Message, OnIntroOK);

                    while (m_Blocked && !FpsGameMode.inGame)
                        yield return null;

                    if (FpsSoloCharacter.localPlayerCharacter == null)
                        m_GameMode.Respawn(m_GameMode.player);
                }
                else
                {
                    // Wait until in game
                    while (!FpsGameMode.inGame)
                        yield return null;

                    yield return null;

                    if (FpsSoloCharacter.localPlayerCharacter == null)
                        m_GameMode.Respawn(m_GameMode.player);
                }
            }
        }

        void OnIntroOK()
        {
            m_Blocked = false;
        }
    }
}