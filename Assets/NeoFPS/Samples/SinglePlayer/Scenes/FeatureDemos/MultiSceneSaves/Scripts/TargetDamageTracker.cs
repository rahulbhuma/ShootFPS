using UnityEngine;
using NeoSaveGames;
using NeoSaveGames.Serialization;
using System;

namespace NeoFPS.Samples.SinglePlayer.MultiScene
{
    public class TargetDamageTracker : MonoBehaviour, IDamageHandler
    {
        public DamageFilter inDamageFilter
        {
            get { return DamageFilter.AllDamageAllTeams; }
            set { }
        }

        public IHealthManager healthManager
        {
            get { return null; }
        }

        public DamageResult AddDamage(float damage)
        {
            TargetHitCounter.ReportHit();
            DamageEvents.ReportDamageHandlerHit(this, null, Vector3.zero, DamageResult.Standard, 1f);
            return DamageResult.Standard;
        }

        public DamageResult AddDamage(float damage, RaycastHit hit)
        {
            TargetHitCounter.ReportHit();
            DamageEvents.ReportDamageHandlerHit(this, null, Vector3.zero, DamageResult.Standard, 1f);
            return DamageResult.Standard;
        }

        public DamageResult AddDamage(float damage, IDamageSource source)
        {
            TargetHitCounter.ReportHit();
            DamageEvents.ReportDamageHandlerHit(this, source, Vector3.zero, DamageResult.Standard, 1f);
            return DamageResult.Standard;
        }

        public DamageResult AddDamage(float damage, RaycastHit hit, IDamageSource source)
        {
            TargetHitCounter.ReportHit();
            DamageEvents.ReportDamageHandlerHit(this, source, Vector3.zero, DamageResult.Standard, 1f);
            return DamageResult.Standard;
        }
    }
}