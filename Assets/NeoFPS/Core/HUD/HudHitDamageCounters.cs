using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoFPS;
using System;

namespace NeoFPS.AI
{
    [HelpURL("https://docs.neofps.com/manual/hudref-mb-hudhitdamagecounters.html")]
    public class HudHitDamageCounters : WorldSpaceHudMarkerBase
    {
        private void OnEnable()
        {
            DamageEvents.onDamageHandlerHit += OnDamageHandlerHit;
        }

        private void OnDisable()
        {
            DamageEvents.onDamageHandlerHit -= OnDamageHandlerHit;
        }

        private void OnDamageHandlerHit(IDamageHandler handler, IDamageSource source, Vector3 hitPoint, DamageResult result, float damage)
        {
            switch (result)
            {
                case DamageResult.Standard:
                    {
                        var marker = GetMarker<HudHitDamageCounterStandard>();
                        marker.Initialise(hitPoint, damage, false);
                    }
                    break;
                case DamageResult.Critical:
                    {
                        var marker = GetMarker<HudHitDamageCounterStandard>();
                        marker.Initialise(hitPoint, damage, true);
                    }
                    break;
                case DamageResult.Blocked:
                    {
                        //var marker = GetMarker<HudHitDamageCounterStandard>();
                        //marker.Initialise(this, hitPoint, damage, false);
                    }
                    break;
            }
        }

        public void ReleaseCounter(HudHitDamageCounterBase counter)
        {
            ReleaseMarker(counter);
        }
    }
}