using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/inventoryref-mb-pickuptriggerzone.html")]
	public class PickupTriggerZone : MonoBehaviour
    {
        [SerializeField, Tooltip("An event that is triggered when the object is used.")]
        private UnityEvent m_OnUsed = new UnityEvent();

        IPickup m_Pickup = null;

        public UnityEvent onUsedUnityEvent
        {
            get { return m_OnUsed; }
        }

        protected void Awake ()
		{
			m_Pickup = GetComponent<IPickup>();
			if (m_Pickup == null)
				Debug.LogError ("ZonePickupTrigger requires IPickup inherited behaviour attached to game object");
			
			Collider c = GetComponent<Collider> ();
			c.isTrigger = true;

            if (gameObject.layer != PhysicsFilter.LayerIndex.TriggerZones)
            {
                Debug.LogWarning("Changing layer on object to Trigger Zones: " + name);
                gameObject.layer = PhysicsFilter.LayerIndex.TriggerZones;
            }
		}

        protected void OnTriggerEnter (Collider other)
		{
			if (other.CompareTag("Player"))
            {
                ICharacter character = other.GetComponent<ICharacter>();
                if (character != null)
                {
                    m_Pickup.Trigger(character);
                    m_OnUsed.Invoke();
                }
            }
		}
	}
}