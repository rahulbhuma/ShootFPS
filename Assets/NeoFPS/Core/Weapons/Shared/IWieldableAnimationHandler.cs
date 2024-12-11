using UnityEngine;

namespace NeoFPS
{
    public interface IWieldableAnimationHandler
    {
        bool isValid { get; }

        void SetInteger(string key, int value);
        void SetInteger(int hash, int value);
        void SetFloat(string key, float value);
        void SetFloat(int hash, float value);
        void SetBool(string key, bool value);
        void SetBool(int hash, bool value);
        void SetTrigger(string key);
        void SetTrigger(int hash);
        void ResetTrigger(string key);
        void ResetTrigger(int hash);
        void SetLayerWeight(int layerIndex, float weight);
    }

    public enum AnimatorLocation
    {
        None,
        AttachedOnly,
        AttachedAndCharacter,
        MultipleAttached,
        MultipleAttachedAndCharacter,
        CharacterOnly
    }

    public class SingleAnimatorHandler : IWieldableAnimationHandler
    {
        Animator m_Animator = null;

        public bool isValid
        {
            get { return m_Animator.isActiveAndEnabled; }
        }

        public SingleAnimatorHandler(Animator animator)
        {
            m_Animator = animator;
        }

        public void SetInteger(string key, int value)
        {
            m_Animator.SetInteger(key, value);
        }

        public void SetInteger(int hash, int value)
        {
            m_Animator.SetInteger(hash, value);
        }

        public void SetFloat(string key, float value)
        {
            m_Animator.SetFloat(key, value);
        }

        public void SetFloat(int hash, float value)
        {
            m_Animator.SetFloat(hash, value);
        }

        public void SetBool(string key, bool value)
        {
            m_Animator.SetBool(key, value);
        }

        public void SetBool(int hash, bool value)
        {
            m_Animator.SetBool(hash, value);
        }

        public void SetTrigger(string key)
        {
            m_Animator.SetTrigger(key);
        }

        public void SetTrigger(int hash)
        {
            m_Animator.SetTrigger(hash);
        }

        public void ResetTrigger(string key)
        {
            m_Animator.SetTrigger(key);
        }

        public void ResetTrigger(int hash)
        {
            m_Animator.SetTrigger(hash);
        }

        public void SetLayerWeight(int layerIndex, float weight)
        {
            if (layerIndex < m_Animator.layerCount)
                m_Animator.SetLayerWeight(layerIndex, weight);
        }
    }

    public class NullAnimatorHandler : IWieldableAnimationHandler
    {
        public bool isValid { get { return false; } }
        public void SetInteger(string key, int value) { }
        public void SetInteger(int hash, int value) { }
        public void SetFloat(string key, float value) { }
        public void SetFloat(int hash, float value) { }
        public void SetBool(string key, bool value) { }
        public void SetBool(int hash, bool value) { }
        public void SetTrigger(string key) { }
        public void SetTrigger(int hash) { }
        public void ResetTrigger(string key) { }
        public void ResetTrigger(int hash) { }
        public void SetLayerWeight(int layerIndex, float weight) { }
    }

    public class MultiAnimatorHandler : IWieldableAnimationHandler
    {
        Animator[] m_Animators = null;
        
        public bool isValid
        {
            get
            {
                for (int i = 0; i < m_Animators.Length; ++i)
                    if (!m_Animators[i].isActiveAndEnabled)
                        return false;
                return true;
            }
        }

        public MultiAnimatorHandler(Animator[] animators)
        {
            m_Animators = animators;
        }

        public void SetInteger(string key, int value)
        {
            int hash = Animator.StringToHash(key);
            for (int i = 0; i < m_Animators.Length; ++i)
                m_Animators[i].SetInteger(hash, value);
        }

        public void SetInteger(int hash, int value)
        {
            for (int i = 0; i < m_Animators.Length; ++i)
                m_Animators[i].SetInteger(hash, value);
        }

        public void SetFloat(string key, float value)
        {
            int hash = Animator.StringToHash(key);
            for (int i = 0; i < m_Animators.Length; ++i)
                m_Animators[i].SetFloat(hash, value);
        }

        public void SetFloat(int hash, float value)
        {
            for (int i = 0; i < m_Animators.Length; ++i)
                m_Animators[i].SetFloat(hash, value);
        }

        public void SetBool(string key, bool value)
        {
            int hash = Animator.StringToHash(key);
            for (int i = 0; i < m_Animators.Length; ++i)
                m_Animators[i].SetBool(hash, value);
        }

        public void SetBool(int hash, bool value)
        {
            for (int i = 0; i < m_Animators.Length; ++i)
                m_Animators[i].SetBool(hash, value);
        }

        public void SetTrigger(string key)
        {
            int hash = Animator.StringToHash(key);
            for (int i = 0; i < m_Animators.Length; ++i)
                m_Animators[i].SetTrigger(hash);
        }

        public void SetTrigger(int hash)
        {
            for (int i = 0; i < m_Animators.Length; ++i)
                m_Animators[i].SetTrigger(hash);
        }

        public void ResetTrigger(string key)
        {
            int hash = Animator.StringToHash(key);
            for (int i = 0; i < m_Animators.Length; ++i)
                m_Animators[i].SetTrigger(hash);
        }

        public void ResetTrigger(int hash)
        {
            for (int i = 0; i < m_Animators.Length; ++i)
                m_Animators[i].SetTrigger(hash);
        }

        public void SetLayerWeight(int layerIndex, float weight)
        {
            for (int i = 0; i < m_Animators.Length; ++i)
            {
                if (layerIndex < m_Animators[i].layerCount)
                    m_Animators[i].SetLayerWeight(layerIndex, weight);
            }
        }
    }
}