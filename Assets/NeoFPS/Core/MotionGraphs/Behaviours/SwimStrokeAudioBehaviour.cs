using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.Parameters;
using NeoFPS.CharacterMotion.States;
using System;
using NeoFPS.Constants;

namespace NeoFPS.CharacterMotion.Behaviours
{
    [MotionGraphElement("Audio/SwimStrokeAudio", "SwimStrokeAudioBehaviour")]
    [HelpURL("https://docs.neofps.com/manual/motiongraphref-mgb-swimstrokeaudiobehaviour.html")]
    public class SwimStrokeAudioBehaviour : MotionGraphBehaviour
    {
        [SerializeField, Tooltip("A selection of audio clips to play. One will be chosen at random each time.")]
        private AudioClip[] m_Clips = { };

        [SerializeField, Range(0, 1), Tooltip("The volume to play the clip at.")]
        private float m_Volume = 1f;

        private ICharacterAudioHandler m_AudioHandler = null;

        public override void Initialise(MotionGraphConnectable o)
        {
            base.Initialise(o);

            // Get audio handler
            if (m_AudioHandler == null)
                m_AudioHandler = controller.GetComponent<ICharacterAudioHandler>();

            if (o is ISwimStroke swim)
                swim.onStroke += OnStroke;
            else
            {
                Debug.LogError("SwimStrokeAudioBehaviour attached to state or sub-graph that doesn't implement ISwimStroke interface");
                enabled = false;
            }
        }

        private void OnStroke(float strength)
        {
            if (m_Clips.Length > 0)
            {
                int index = UnityEngine.Random.Range(0, m_Clips.Length);
                m_AudioHandler.PlayClip(m_Clips[index], FpsCharacterAudioSource.Body, m_Volume);
            }
        }
    }
}