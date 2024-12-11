using UnityEngine;

namespace NeoFPS
{ 
    public interface IMeleeHitExtension
    {
        void OnMeleeHit(RaycastHit hit);
    }
}
