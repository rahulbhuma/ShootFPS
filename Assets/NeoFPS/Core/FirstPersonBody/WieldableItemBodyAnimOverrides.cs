using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS
{
    [RequireComponent(typeof(IWieldable))]
    [HelpURL("https://docs.neofps.com/manual/inventoryref-mb-wieldableitembodyanimoverrides.html")]
    public class WieldableItemBodyAnimOverrides : MonoBehaviour
    {
        [SerializeField, Tooltip("The character profile is an asset that is connected to an animator controller and exposes specific clips to override")]
        private FirstPersonCharacterAnimationProfile m_CharacterProfile = null;

        [SerializeField, Tooltip("The clips to replace the character profile clips with")]
        private ClipReplacement[] m_Clips = { };

        [Serializable]
        private struct ClipReplacement
        {
            public AnimationClip original;
            public AnimationClip replacement;
        }

        public List<KeyValuePair<AnimationClip, AnimationClip>> overrides
        {
            get;
            private set;
        }

        private void OnValidate()
        {
            if (m_CharacterProfile == null)
            {
                if (m_Clips.Length > 0)
                    m_Clips = new ClipReplacement[0];
            }
            else
            {
                if (m_Clips.Length != m_CharacterProfile.clips.Length)
                {
                    var temp = new ClipReplacement[m_CharacterProfile.clips.Length];
                    for (int i = 0; i < m_Clips.Length && i < temp.Length; ++i)
                    {
                        temp[i].original = m_Clips[i].original;
                        temp[i].replacement = m_Clips[i].replacement;
                    }
                    m_Clips = temp;
                }
            }
        }

        private void Awake()
        {
            // Build overrides list
            overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(m_Clips.Length);
            for (int i = 0; i < m_Clips.Length; ++i)
            {
                if (m_Clips[i].original != null && m_Clips[i].replacement != null)
                    overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(m_Clips[i].original, m_Clips[i].replacement));
            }
        }
    }
}