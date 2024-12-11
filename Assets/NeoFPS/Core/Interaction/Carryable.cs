using NeoSaveGames;
using NeoSaveGames.Serialization;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS
{
    [RequireComponent (typeof(Rigidbody))]
    [HelpURL("https://docs.neofps.com/manual/interactionref-mb-carryable.html")]
    public class Carryable : MonoBehaviour
    {
        [SerializeField, Tooltip("An offset applied between the transform position of this object and the character's carry anchor point. Use this to prevent larger objects filling the screen.")]
        private Vector3 m_Offset = Vector3.zero;

        [SerializeField, Tooltip("Should the object be rotated so it is the correct way up when picked up.")]
        private bool m_ReorientOnPickup = false;

        [SerializeField, Tooltip("An orientation offset that should be applied to the carryable when first picked up.")]
        private Vector3 m_OrientationOffset = Vector3.zero;

        [SerializeField, Tooltip("Can the object be manually rotated.")]
        private bool m_Manipulatable = false;

        [Header("Feedback")]

        [SerializeField, Tooltip("An audio clip played when the object is picked up.")]
        private AudioClip m_PickUpAudio = null;

        [SerializeField, Tooltip("An audio clip played when the object is dropped.")]
        private AudioClip m_DropAudio = null;

        [SerializeField, Tooltip("An event that is triggered when the object is picked up. Example uses are to disable an automated turret, or fold up a machine.")]
        private UnityEvent m_OnPickedUp = new UnityEvent();

        [SerializeField, Tooltip("An event that is triggered when the object is dropped. An example use would be to deploy an automated turret when dropped on stable ground.")]
        private UnityEvent m_OnDropped = new UnityEvent();

        public event UnityAction onPickedUp
        {
            add { m_OnPickedUp.AddListener(value); }
            remove { m_OnPickedUp.RemoveListener(value); }
        }

        public event UnityAction onDropped
        {
            add { m_OnDropped.AddListener(value); }
            remove { m_OnDropped.RemoveListener(value); }
        }

        public Vector3 centerOffset
        {
            get { return m_Offset; }
        }

        public bool manipulatable
        {
            get { return m_Manipulatable; }
        }

        public virtual bool CanCarry()
        {
            return true;
        }

        public Quaternion GetStartingOrientation(Quaternion current)
        {
            if (m_ReorientOnPickup)
                return Quaternion.Euler(m_OrientationOffset);
            else
                return current;
        }

        public void OnPickedUp (ICarrySystem carrier)
        {
            // Check the carrier is valid
            if (carrier == null)
                return;

            // Play the pick up audio
            if (m_PickUpAudio != null)
                NeoFpsAudioManager.PlayEffectAudioAtPosition(m_PickUpAudio, transform.position);

            // Fire unity event
            m_OnPickedUp.Invoke();
        }

        public void OnDropped(ICarrySystem carrier)
        {
            // Check the carrier is valid
            if (carrier == null)
                return;

            // Play the pick up audio
            if (m_DropAudio != null)
                NeoFpsAudioManager.PlayEffectAudioAtPosition(m_DropAudio, transform.position);

            // Fire unity event
            m_OnDropped.Invoke();
        }

        private void Reset()
        {
            m_Offset = GetComponent<Rigidbody>().centerOfMass;
        }
    }
}
