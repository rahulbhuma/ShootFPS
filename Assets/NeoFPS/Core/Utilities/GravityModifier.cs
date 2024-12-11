using NeoFPS.SinglePlayer;
using UnityEngine;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/motiongraphref-mb-gravitymodifier.html")]
    public class GravityModifier : MonoBehaviour
    {
        [SerializeField, Tooltip("The gravity vector to apply to the player character (and optionally Unity phyics).")]
        private Vector3 m_Gravity = new Vector3(0f, -9.82f, 0f);

        [SerializeField, Tooltip("Should this behaviour also set the Unity physics vector on top of the character gravity vector.")]
        private bool m_SetPhysicsGravity = false;

        public void SetGravity()
        {
            SetGravity(m_Gravity, m_SetPhysicsGravity);
        }

        public void SetGravity(Vector3 gravity, bool setPhysicsGravity)
        {
            var character = FpsSoloCharacter.localPlayerCharacter;
            if (character != null)
                character.motionController.characterController.characterGravity.gravity = gravity;

            if (m_SetPhysicsGravity)
                Physics.gravity = gravity;
        }
    }
}