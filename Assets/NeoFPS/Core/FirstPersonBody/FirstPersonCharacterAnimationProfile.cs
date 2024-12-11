using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/weaponref-so-firstpersoncharacteranimationprofile.html")]
    [CreateAssetMenu(fileName = "FirstPersonCharacterAnimationProfile", menuName = "NeoFPS/First-Person Character Animation Profile", order = NeoFpsMenuPriorities.ungrouped_characteranimationprofile)]
    public class FirstPersonCharacterAnimationProfile : ScriptableObject
    {
        [SerializeField, Tooltip("The animator controller that you want to search through and expose animation clips from.")]
        private RuntimeAnimatorController m_Controller = null;

        [SerializeField]
        private ClipInfo[] m_Clips = {};
        
        [Serializable]
        public struct ClipInfo
        {
            public AnimationClip clip;
            public string description;
        }

        public ClipInfo[] clips
        {
            get { return m_Clips; }
        }
        
        public bool CheckIsValid()
        {
            // Basic checks
            if (m_Controller == null)
                return false;
            if (m_Clips.Length == 0)
                return false;

            // Check for duplicate / invalid descriptions
            var descriptions = new HashSet<string>();
            for (int i = 0; i < m_Clips.Length; ++i)
            {
                if (string.IsNullOrWhiteSpace(m_Clips[i].description))
                    return false;
                if (descriptions.Contains(m_Clips[i].description))
                    return false;
                else
                    descriptions.Add(m_Clips[i].description);
            }

            return true;
        }
    }
}