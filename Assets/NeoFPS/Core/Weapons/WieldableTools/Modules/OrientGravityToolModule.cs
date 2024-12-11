using NeoFPS.CharacterMotion.Parameters;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoCC;

namespace NeoFPS.WieldableTools
{
    [HelpURL("https://docs.neofps.com/manual/weaponsref-mb-orientgravitytoolmodule.html")]
    public class OrientGravityToolModule : BaseWieldableToolModule
    {
        [SerializeField, Tooltip("The gravity acceleration strength (meters per second squared).")]
        private float m_GravityStrength = 9.82f;
        [SerializeField, Tooltip("The physics layers the tool can detect and orient to.")]
        private LayerMask m_CollisionLayers = PhysicsFilter.Masks.CharacterBlockers;
        [SerializeField, Tooltip("The maximum distance that the tool can detect a valid surface.")]
        private float m_MaxDistance = 500f;

        [Header("Markers")]
        [SerializeField, NeoObjectInHierarchyField(false), Tooltip("An object in the tool's hierarchy to use as the ground position marker for the blink target (the object will be moved out of the tool's hierarchy).")]
        private Transform m_GroundMarker = null;

        const float k_TinyValue = 0.001f;

        private Vector3 m_GroundPoint = Vector3.zero;
        private Vector3 m_GroundNormal = Vector3.zero;
        private NeoCharacterController m_CharacterController = null;
        private bool m_Checking = false;
        private bool m_ValidTarget = false;
        private int m_SnapCounter = 0;

        public float maxDistance
        {
            get { return m_MaxDistance; }
            set { m_MaxDistance = value; }
        }

        public override WieldableToolActionTiming timing
        {
            get { return k_TimingsStartAndEnd; }
        }

        public override bool isValid
        {
            get { return m_CollisionLayers != 0 && m_CharacterController != null; }
        }

        protected void OnValidate()
        {
            m_MaxDistance = Mathf.Clamp(m_MaxDistance, 1f, 1000f);
        }

        public override void Initialise(IWieldableTool t)
        {
            base.Initialise(t);

            if (t.wielder != null)
                m_CharacterController = t.wielder.GetComponent<NeoCharacterController>();
            else
                m_CharacterController = null;

            // Move markers out of hierarchy
            if (m_GroundMarker != null)
            {
                m_GroundMarker.gameObject.SetActive(false);
                m_GroundMarker.localScale *= 2f;
            }
        }

        public override void FireStart()
        {
            m_Checking = true;
            m_ValidTarget = false;
        }

        public override void FireEnd(bool success)
        {
            if (m_ValidTarget && success)
            {
                // Set the gravity direction
                m_SnapCounter = 5;

                // Hide the visuals
                HideMarkers();
            }

            m_Checking = false;
        }

        protected void OnDisable()
        {
            HideMarkers();
        }

        protected void OnDestroy()
        {
            if (m_GroundMarker != null)
            {
                Destroy(m_GroundMarker.gameObject);
                m_GroundMarker = null;
            }
        }

        public override void Interrupt()
        {
            // Hide the visuals
            HideMarkers();
            m_ValidTarget = false;
            m_Checking = false;
        }

        protected void LateUpdate()
        {
            if (m_Checking)
                CheckForTargets();
        }

        private void FixedUpdate()
        {
            if (m_SnapCounter > 0)
            {
                m_CharacterController.AddForce(m_CharacterController.up * 20f, ForceMode.Acceleration, true);
                --m_SnapCounter;
                if (m_SnapCounter == 0)
                    m_CharacterController.gravity = m_GroundNormal * -m_GravityStrength;
            }
        }

        public override bool TickContinuous()
        {
            return true;
        }

        void CheckForTargets()
        {
            var motionController = tool.wielder.motionController;
            var localTransform = motionController.localTransform;
            var characterController = motionController.characterController;
            var up = characterController.up;
            var aimRay = tool.wielder.fpCamera.GetAimRay();

            RaycastHit hit;

            // Check if aim ray hits
            if (PhysicsExtensions.RaycastNonAllocSingle(aimRay, out hit, m_MaxDistance, m_CollisionLayers, localTransform, QueryTriggerInteraction.Ignore))
            {
                // Get the angle from up of the hit normal
                var angle = Vector3.Angle(hit.normal, up);

                // Valid ground hit
                m_GroundPoint = hit.point + hit.normal * 0.05f;
                m_GroundNormal = hit.normal;

                // Show the visuals
                if (m_GroundMarker != null)
                {
                    m_GroundMarker.gameObject.SetActive(true);
                    m_GroundMarker.position = m_GroundPoint;
                    m_GroundMarker.rotation = Quaternion.FromToRotation(Vector3.up, m_GroundNormal);
                }

                m_ValidTarget = true;
            }
            else
            {
                // Hide the visuals
                HideMarkers();
                m_ValidTarget = false;
            }
        }

        void HideMarkers()
        {
            m_GroundMarker?.gameObject.SetActive(false);
        }
    }
}