using NeoFPS.Constants;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/inventoryref-mb-playaudioinstantaction.html")]
    public class PlayAudioInstantAction : FpsInventoryInstantUseAction
    {
        [SerializeField, Tooltip("The clip to play.")]
        private AudioClip m_Clip = null;
        [SerializeField, Range(0f, 1f), Tooltip("The volume for the clip.")]
        private float m_Volume = 1f;

        FpsCharacterAudioHandler m_AudioHandler = null;

        public override void Initialise(FpsInventoryItemBase item)
        {
            base.Initialise(item);

            m_AudioHandler = item.GetComponentInParent<FpsCharacterAudioHandler>();
        }

        public override void PerformAction()
        {
            if (m_AudioHandler != null && m_Clip != null)
                m_AudioHandler.PlayClip(m_Clip, FpsCharacterAudioSource.Head, m_Volume);
        }
    }
}