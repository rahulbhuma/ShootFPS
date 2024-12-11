using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/healthref-mb-contactdamagesend.html")]
    public class ContactDamageSend : MonoBehaviour, IDamageSource
    {
        [SerializeField, Tooltip("The amount of damage to apply to the player character.")]
        private float m_Damage = 10f;
        [SerializeField, Tooltip("The type of damage to apply.")]
        private DamageType m_DamageType = DamageType.Default;
        [SerializeField, Tooltip("A description of the damage to use in logs, etc.")]
        private string m_DamageDescription = "Damage Zone";

        private void OnCollisionEnter(Collision collision)
        {
            var root = collision.transform.root;
            if (root.CompareTag("Player"))
            {
                ICharacter c = root.GetComponentInParent<ICharacter>();
                if (c != null)
                {
                    var healthManager = c.GetComponent<IHealthManager>();
                    if (healthManager != null)
                        healthManager.AddDamage(m_Damage, false, this);
                }
            }
            else
            {
                if (root.CompareTag("AI"))
                {
                    var damageHandler = collision.collider.GetComponent<IDamageHandler>();
                    if (damageHandler != null && damageHandler.enabled)
                    {
                        damageHandler.AddDamage(m_Damage, this);
                    }
                    else
                    {
                        var healthManager = root.GetComponent<IHealthManager>();
                        if (healthManager != null)
                            healthManager.AddDamage(m_Damage);
                    }
                }
            }
        }

        #region IDamageSource IMPLEMENTATION

        private DamageFilter m_OutDamageFilter = DamageFilter.AllDamageAllTeams;

        public DamageFilter outDamageFilter
        {
            get { return new DamageFilter(m_DamageType, DamageTeamFilter.All); }
            set { m_OutDamageFilter = value; }
        }

        public IController controller
        {
            get { return null; }
        }

        public Transform damageSourceTransform
        {
            get { return transform; }
        }

        public string description
        {
            get { return m_DamageDescription; }
        }

        #endregion
    }
}