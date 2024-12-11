using NeoFPS.CharacterMotion.Parameters;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoCC;

namespace NeoFPS.WieldableTools
{
    [HelpURL("https://docs.neofps.com/manual/weaponsref-mb-blinktoolmodule.html")]
    public class BlinkToolModule : BaseWieldableToolModule
    {
        [SerializeField, Tooltip("The name of the vector parameter on the character's motion graph that you want to set the blink target to.")]
        private string m_BlinkTargetKey = "blinkTarget";
        [SerializeField, Tooltip("The name of the trigger parameter on the character's motion graph that you want to set when the blink is activated.")]
        private string m_BlinkTriggerKey = "blink";
        [SerializeField, Tooltip("The physics layers the tool can blink onto.")]
        private LayerMask m_CollisionLayers = PhysicsFilter.Masks.CharacterBlockers;
        [SerializeField, Tooltip("The maximum distance that the character can blink.")]
        private float m_MaxDistance = 25f;
        [SerializeField, Tooltip("The last valid blink target will be stored this long and used. Makes the target selection more forgiving.")]
        private float m_CoyoteTime = 0.25f;

        [Header("Ground Blink")]

        [SerializeField, Tooltip("The maximum slope that a ground plane can have for you to blink onto it.")]
        private float m_MaxGroundSlope = 45f;

        [Header("Ledge Blink")]

        [SerializeField, Tooltip("The maximum distance down a wall you can look and it will blink to the top ledge.")]
        private float m_MaxClimbHeight = 0.25f;
        [SerializeField, Tooltip("If looking at the top of a wall, this is the distance past the edge to overshoot when blinking onto the ledge.")]
        private float m_LedgeOvershoot = 0.05f;
        [SerializeField, Tooltip("The maximum angle away from vertical that the blink tool can register as a wall with a ledge.")]
        private float m_MaxWallAngle = 5f;
        [SerializeField, Tooltip("An angle range centered on the wall normal that you can blink within. At 180 degrees you can blink to any ledge. At 0 degrees you would have to be looking perfectly flat on to the wall to blink to its ledge.")]
        private float m_YawLimit = 90f;

        [Header("Markers")]
        [SerializeField, NeoObjectInHierarchyField(false), Tooltip("An object in the tool's hierarchy to use as the ground position marker for the blink target (the object will be moved out of the tool's hierarchy).")]
        private Transform m_GroundMarker = null;
        [SerializeField, NeoObjectInHierarchyField(false), Tooltip("An object in the tool's hierarchy to use as the ledge position marker for the blink target (the object will be moved out of the tool's hierarchy).")]
        private Transform m_LedgeMarker = null;

        const float k_TinyValue = 0.001f;

        private VectorParameter m_BlinkTargetParameter = null;
        private TriggerParameter m_BlinkTriggerParameter = null;
        private Collider[] m_OverlapColliders = new Collider[1];
        private TargetState m_TargetState = TargetState.None;
        private Vector3 m_GroundPoint = Vector3.zero;
        private Vector3 m_GroundNormal = Vector3.up;
        private Vector3 m_WallNormal = Vector3.up;
        private Vector3 m_WallPoint = Vector3.zero;
        private float m_Timeout = 0f;
        private bool m_Checking = false;

        enum TargetState
        {
            None,
            Floor,
            Wall
        }

        public float maxDistance
        {
            get { return m_MaxDistance; }
            set { m_MaxDistance = value; }
        }

        public float maxClimbHeight
        {
            get { return m_MaxClimbHeight; }
            set { m_MaxClimbHeight = value; }
        }

        public override WieldableToolActionTiming timing
        {
            get { return k_TimingsAll; }
        }

        public override bool isValid
        {
            get { return m_CollisionLayers != 0; }
        }

        protected void OnValidate()
        {
            m_MaxDistance = Mathf.Clamp(m_MaxDistance, 1f, 1000f);
            m_MaxWallAngle = Mathf.Clamp(m_MaxWallAngle, 0.1f, 30f);
            m_YawLimit = Mathf.Clamp(m_YawLimit, 10f, 180f);
        }

        public override void Initialise(IWieldableTool t)
        {
            base.Initialise(t);

            if (t.wielder != null)
            {
                m_BlinkTargetParameter = t.wielder.motionController.motionGraph.GetVectorProperty(m_BlinkTargetKey);
                m_BlinkTriggerParameter = t.wielder.motionController.motionGraph.GetTriggerProperty(m_BlinkTriggerKey);
            }

            if (m_BlinkTargetParameter == null)
            {
                Debug.LogError("Failed to get blink target vector parameter from the wielder's motion graph.");
                enabled = false;
            }

            // Move markers out of hierarchy
            if (m_GroundMarker != null)
            {
                m_GroundMarker.gameObject.SetActive(false);
                //m_GroundMarker.SetParent(null);
                m_GroundMarker.localScale *= 2f;// Vector3.one;
            }
            if (m_LedgeMarker != null)
            {
                m_LedgeMarker.gameObject.SetActive(false);
                //m_LedgeMarker.SetParent(null);
                m_LedgeMarker.localScale *= 2f;// Vector3.one;
            }
        }

        public override void FireStart()
        {
            m_TargetState = TargetState.None;
            m_Checking = true;
            m_Timeout = 0f;
        }

        public override void FireEnd(bool success)
        {
            if (success && (m_TargetState == TargetState.Floor || m_TargetState == TargetState.Wall))
            {
                // Set the parameters
                m_BlinkTargetParameter.value = m_GroundPoint;
                if (m_BlinkTriggerParameter != null)
                    m_BlinkTriggerParameter.Trigger();

                // Hide the visuals
                HideMarkers();

                // Reset the values
                m_TargetState = TargetState.None;
                m_Timeout = 0f;
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
            if (m_LedgeMarker != null)
            {
                Destroy(m_LedgeMarker.gameObject);
                m_LedgeMarker = null;
            }
        }

        public override void Interrupt()
        {
            m_TargetState = TargetState.None;
            m_Checking = false;
            m_Timeout = 0f;

            // Hide the visuals
            HideMarkers();
        }

        protected void LateUpdate()
        {
            if (m_Checking)
                CheckForTargets(false);
        }

        public override bool TickContinuous()
        {
            return CheckForTargets(true);
        }

        bool CheckForTargets(bool fixedUpdate)
        {
            TargetState newTargetState = TargetState.None;

            var motionController = tool.wielder.motionController;
            var localTransform = motionController.localTransform;
            var characterController = motionController.characterController;
            var height = characterController.height;
            var radius = characterController.radius;
            var up = characterController.up;
            var skinWidth = characterController.skinWidth;
            var aimRay = tool.wielder.fpCamera.GetAimRay();
            var startPosition = localTransform.position;
            var eyeLocalPosition = tool.wielder.fpCamera.cameraTransform.position - startPosition;
            var newGroundPoint = Vector3.zero;
            var newGroundNormal = Vector3.up;
            var newWallPoint = Vector3.zero;
            var newWallNormal = Vector3.up;

            RaycastHit hit;

            // Check if aim ray hits
            if (PhysicsExtensions.RaycastNonAllocSingle(aimRay, out hit, m_MaxDistance, m_CollisionLayers, localTransform, QueryTriggerInteraction.Ignore))
            {
                // Get the angle from up of the hit normal
                var angle = Vector3.Angle(hit.normal, up);

                // Ground hit
                if (angle <= m_MaxGroundSlope)
                {
                    Vector3 bottom = hit.point + hit.normal * (radius + skinWidth);
                    Vector3 top = bottom + up * (height - 2f * radius);

                    // Check for capsule overlap (none means it's clear to blink there)
                    if (Physics.OverlapCapsuleNonAlloc(bottom, top, radius, m_OverlapColliders, m_CollisionLayers, QueryTriggerInteraction.Ignore) == 0)
                    {
                        // Valid ground hit
                        newGroundPoint = hit.point + hit.normal * (skinWidth * 0.5f);
                        newGroundNormal = hit.normal;
                        newTargetState = TargetState.Floor;
                    }
                }
                else
                {
                    // Wall hit
                    if (angle > 90f - m_MaxWallAngle - k_TinyValue &&
                        angle < 90f + m_MaxWallAngle + k_TinyValue &&
                        (m_YawLimit > 179f || Vector3.Angle(Vector3.ProjectOnPlane(hit.normal, up).normalized, -characterController.forward) < m_YawLimit * 0.5f))
                    {
                        // Record the wall normal and point
                        newWallNormal = hit.normal;
                        newWallPoint = hit.point;

                        // Get cast values for character capsule check
                        Vector3 projectedUp = Vector3.ProjectOnPlane(up, newWallNormal).normalized;
                        Vector3 sphere = hit.point + hit.normal * (radius + skinWidth);
                        Vector3 ledgeStepVector = -hit.normal;
                        float checkDistance = height - radius + m_MaxClimbHeight + skinWidth;
                        float moved;

                        // Sphere cast up the wall from the sphere point to check for a ceiling hit
                        if (PhysicsExtensions.SphereCastNonAllocSingle(
                            new Ray(sphere, projectedUp),
                            radius,
                            out hit,
                            checkDistance,
                            m_CollisionLayers,
                            localTransform,
                            QueryTriggerInteraction.Ignore
                            ))
                        {
                            // Get new sphere point
                            sphere = hit.point + hit.normal * (radius + skinWidth);

                            // Get distance moved
                            moved = hit.distance;

                            // Check if forwards move is into ceiling
                            float stepDotNormal = Vector3.Dot(ledgeStepVector, hit.normal);
                            if (stepDotNormal < 0f)
                            {
                                // Adjust step vector to prevent intersect
                                ledgeStepVector = Vector3.ProjectOnPlane(ledgeStepVector, hit.normal).normalized;

                                // Remove ceiling intersect adjustments from distance moved
                                moved += stepDotNormal * m_LedgeOvershoot;
                            }
                        }
                        else
                        {
                            // No obstruction overhead
                            sphere += projectedUp * checkDistance;
                            moved = checkDistance;
                        }

                        // Sphere cast forwards to get overshoot over ledge (cancel if hit something)
                        if (!PhysicsExtensions.SphereCastNonAllocSingle(
                            new Ray(sphere, ledgeStepVector),
                            radius,
                            out hit,
                            m_LedgeOvershoot + radius,
                            m_CollisionLayers,
                            localTransform,
                            QueryTriggerInteraction.Ignore
                            ))
                        {
                            // Get new start point
                            sphere += ledgeStepVector * (m_LedgeOvershoot + radius);

                            // Sphere cast down to get ledge ground
                            if (PhysicsExtensions.SphereCastNonAllocSingle(
                                new Ray(sphere, -projectedUp),
                                radius,
                                out hit,
                                moved + k_TinyValue,
                                m_CollisionLayers,
                                localTransform,
                                QueryTriggerInteraction.Ignore
                                ))
                            {
                                // Get the angle from up of the hit normal
                                angle = Vector3.Angle(hit.normal, up);

                                // Ground hit and moved far enough to fit body
                                if (angle <= m_MaxGroundSlope && hit.distance >= (height - radius - radius))
                                {
                                    Vector3 bottom = hit.point + hit.normal * (radius + skinWidth);
                                    Vector3 top = bottom + up * (height - 2f * radius);

                                    // Check for capsule overlap (none means it's clear to blink there)
                                    if (Physics.OverlapCapsuleNonAlloc(bottom, top, radius, null, m_CollisionLayers, QueryTriggerInteraction.Ignore) == 0)
                                    {
                                        // Valid ground hit
                                        newTargetState = TargetState.Wall;

                                        // Get a decent target position (near contact)
                                        newGroundPoint = hit.point + hit.normal * (skinWidth * 0.5f);
                                        newGroundNormal = hit.normal;

                                        // Move wall point up to ledge plane
                                        var plane = new Plane(newWallNormal, newWallPoint);
                                        newWallPoint = plane.ClosestPointOnPlane(newGroundPoint);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Line of sight check
            if (newTargetState != TargetState.None)
            {
                // Check for collisions along eyeline
                var offsetVector = newGroundPoint - startPosition;
                if (PhysicsExtensions.SphereCastFiltered(new Ray(startPosition + eyeLocalPosition, offsetVector), radius * 0.25f, offsetVector.magnitude, m_CollisionLayers, localTransform))
                    newTargetState = TargetState.None;
            }
            else
            {
                if (m_TargetState != TargetState.None)
                {
                    // Check for collisions along eyeline
                    var offsetVector = m_GroundPoint - startPosition;
                    if (PhysicsExtensions.SphereCastFiltered(new Ray(startPosition + eyeLocalPosition, offsetVector), radius * 0.25f, offsetVector.magnitude, m_CollisionLayers, localTransform))
                    {
                        m_TargetState = TargetState.None;
                        // Hide the visuals
                        HideMarkers();
                    }
                }
            }

            switch (newTargetState)
            {
                case TargetState.Wall:
                    m_TargetState = newTargetState;
                    m_Timeout = m_CoyoteTime;

                    m_GroundPoint = newGroundPoint;
                    m_GroundNormal = newGroundNormal;
                    m_WallPoint = newWallPoint;
                    m_WallNormal = newWallNormal;

                    // Show the visuals
                    if (m_GroundMarker != null)
                    {
                        m_GroundMarker.gameObject.SetActive(true);
                        m_GroundMarker.position = m_GroundPoint;
                        m_GroundMarker.rotation = Quaternion.FromToRotation(Vector3.up, m_GroundNormal);
                    }
                    if (m_LedgeMarker != null)
                    {
                        m_LedgeMarker.gameObject.SetActive(true);
                        m_LedgeMarker.position = m_WallPoint;
                        m_LedgeMarker.rotation = Quaternion.LookRotation(m_WallNormal);
                    }

                    break;
                case TargetState.Floor:
                    m_TargetState = newTargetState;
                    m_Timeout = m_CoyoteTime;

                    m_GroundPoint = newGroundPoint;
                    m_GroundNormal = newGroundNormal;

                    // Show the visuals
                    m_LedgeMarker?.gameObject.SetActive(false);
                    if (m_GroundMarker != null)
                    {
                        m_GroundMarker.gameObject.SetActive(true);
                        m_GroundMarker.position = m_GroundPoint;
                        m_GroundMarker.rotation = Quaternion.FromToRotation(Vector3.up, m_GroundNormal);
                    }

                    break;
                default:
                    if (fixedUpdate)
                    {
                        m_Timeout -= Time.deltaTime;
                        if (m_Timeout < 0f)
                        {
                            m_TargetState = TargetState.None;

                            // Hide the visuals
                            HideMarkers();
                        }
                    }
                    if (m_TargetState != TargetState.None)
                    {
                        // Show the visuals
                        if (m_GroundMarker != null)
                        {
                            m_GroundMarker.position = m_GroundPoint;
                            m_GroundMarker.rotation = Quaternion.FromToRotation(Vector3.up, m_GroundNormal);
                        }
                        if (m_LedgeMarker != null)
                        {
                            m_LedgeMarker.position = m_WallPoint;
                            m_LedgeMarker.rotation = Quaternion.LookRotation(m_WallNormal);
                        }
                    }
                    break;
            }


            return m_TargetState != TargetState.None;
        }

        void HideMarkers()
        {
            m_LedgeMarker?.gameObject.SetActive(false);
            m_GroundMarker?.gameObject.SetActive(false);
        }
    }
}