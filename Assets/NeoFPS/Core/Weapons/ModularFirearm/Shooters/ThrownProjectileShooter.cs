using UnityEngine;
using NeoSaveGames.Serialization;
using NeoSaveGames;

namespace NeoFPS.ModularFirearms
{
    [HelpURL("https://docs.neofps.com/manual/weaponsref-mb-thrownprojectileshooter.html")]
    public class ThrownProjectileShooter : BaseShooterBehaviour
    {
        [SerializeField, NeoObjectInHierarchyField(true, required = true), Tooltip("A proxy transform for setting the position and rotation of the spawned projectile.")]
        private Transform m_MuzzleTip = null;

        [SerializeField, NeoPrefabField(typeof(ThrownWeaponProjectile)), Tooltip("The prefab to throw.")]
        private PooledObject m_SpawnedProjectile = null;
        
        [SerializeField, Tooltip("The starting speed of the projectile (in the forward direction of the muzzle tip).")]
        private float m_MuzzleVelocity = 50f;
        
        public override void Shoot(float accuracy, IAmmoEffect effect)
        {
            var projectile = PoolManager.GetPooledObject<ThrownWeaponProjectile>(m_SpawnedProjectile, m_MuzzleTip.position, m_MuzzleTip.rotation);
            
            Vector3 velocity = m_MuzzleTip.forward * m_MuzzleVelocity;

            projectile.Throw(velocity, firearm as IDamageSource);
            
            base.Shoot(accuracy, effect);
        }
    }
}