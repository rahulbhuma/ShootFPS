using System;
using UnityEngine;
using NeoSaveGames.Serialization;
using NeoSaveGames;
using UnityEngine.Events;

namespace NeoFPS
{
    [RequireComponent(typeof(Animator))]
    public class DeathAnimation : MonoBehaviour
    {
        [SerializeField, AnimatorParameterKey(AnimatorControllerParameterType.Bool), Tooltip("The bool parameter on the animator to set when the character dies.")]
        private string m_DeadParameter = "dead";

        private Animator m_Animator = null;
        private int m_DeadHash = -1;

        private void Awake()
        {
            m_DeadHash = Animator.StringToHash(m_DeadParameter);

            var healthManager = GetComponentInParent<IHealthManager>();
            if (healthManager != null)
            {
                m_Animator = GetComponent<Animator>();
                healthManager.onIsAliveChanged += OnIsAliveChanged;
            }
            else
                Debug.LogError("DeathAnimation component is attached to an animator with no health manager in its parent hierarchy", gameObject);
        }

        private void OnIsAliveChanged(bool alive)
        {
            m_Animator.SetBool(m_DeadHash, !alive);
        }
    }
}