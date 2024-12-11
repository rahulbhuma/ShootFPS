using NeoCC;
using NeoSaveGames;
using NeoSaveGames.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/interactionref-mb-rigidbodycarrysystem.html")]
    public class RigidbodyCarrySystem : CarrySystemBase
    {
        [Header("Physics")]

        [SerializeField, Tooltip("The anchor transform that carried objects will snap to. This should be a child of an object with the default carry position so that it can move and rotate relative.")]
        private Transform m_Anchor = null;

        [SerializeField, Tooltip("The max movement speed for the carried object to match the anchor position.")]
        private float m_CarrySpeed = 50f;

        [SerializeField, Tooltip("An event fired when the character picks an object up.")]
        private float m_PositionBlend = 0.85f;

        [SerializeField, Tooltip("A transform representing the throw direction (forwards / Z-axis). This would usually be the parent of the anchor transform.")]
        private Transform m_ThrowDirection = null;

        [SerializeField, Tooltip("The force to apply in the throw direction to the carried object when throwing it.")]
        private float m_ThrowForce = 10f;

        [SerializeField, Tooltip("How the throw force should be applied (eg ignoring mass).")]
        private ForceMode m_ThrowForceMode = ForceMode.VelocityChange;

        [SerializeField, Tooltip("A distance from the anchor where the break timer will start. Once the break timer hits the break duration, the object will be dropped. If the object moves back within the break distance then the timer resets.")]
        private float m_BreakDistance = 0.75f;

        [SerializeField, Tooltip("If the carried object is further from the anchor than the break distance for this amount of time then it will be dropped.")]
        private float m_BreakDuration = 0.75f;

        [Header("Events")]

        [SerializeField, Tooltip("An event fired when the character picks an object up.")]
        private CarryableEvent m_OnPickedObjectUp = new CarryableEvent();

        [SerializeField, Tooltip("An event fired when the character picks an object up.")]
        private CarryableEvent m_OnDroppedObject = new CarryableEvent();


        [Serializable]
        private class CarryableEvent : UnityEvent<Rigidbody> { }

        private List<Collider> m_Colliders = new List<Collider>(8);
        private CapsuleCollider m_CharacterCollider = null;
        private NeoCharacterController m_CharacterController = null;
        private RigidbodyInterpolation m_OldInterpolation;
        private bool m_OldGravity = false;
        private float m_BreakTimer = 0f;
        private Quaternion m_Rotation = Quaternion.identity;
        private float m_CurrentAnchorZ = 0f;

        protected void OnValidate()
        {
            m_PositionBlend = Mathf.Clamp(m_PositionBlend, 0.05f, 1.5f);
        }

        protected new void Awake()
        {
            base.Awake();

            m_CharacterCollider = GetComponent<CapsuleCollider>();
            m_CharacterController = GetComponent<NeoCharacterController>();

            if (m_ThrowDirection == null)
                m_ThrowDirection = m_Anchor.parent;
        }

        protected override bool CanCarryTarget(Rigidbody target)
        {
            // Can't carry kinematic rigidbodies
            if (target.isKinematic)
                return false;

            // Can't pick up objects attached to spring or hinge joint
            if (target.TryGetComponent(out Joint joint))
            {
                if (joint is SpringJoint || joint is FixedJoint)
                    return false;
            }

            return true;
        }

        void ProcessRigidbody()
        {
            // Apply required interpolation and gravity settings
            m_OldInterpolation = carryTarget.interpolation;
            m_OldGravity = carryTarget.useGravity;
            carryTarget.interpolation = RigidbodyInterpolation.Interpolate;
            carryTarget.useGravity = false;

            // Set carried object colliders to ignore collisions with character collider
            carryTarget.GetComponentsInChildren(false, m_Colliders);
            for (int i = 0; i < m_Colliders.Count; ++i)
                Physics.IgnoreCollision(m_CharacterCollider, m_Colliders[i], true);
            m_Colliders.Clear();

            // Set character controller to ignore collisions with carried object
            m_CharacterController.SetIgnoreRigidbody(carryTarget);

            // Set inertia tensor for smooth rotation
            carryTarget.inertiaTensor = new Vector3(1f, 1f, 1f);
        }

        protected override void OnObjectPickedUp()
        {
            // Process the rigidbody for carry
            ProcessRigidbody();

            // Match anchor to object
            m_Anchor.position = GetTargetWorldCenter();
            m_Anchor.rotation = carryTarget.rotation;
            m_Rotation = GetStartingOrientation(m_Anchor.localRotation);
            m_CurrentAnchorZ = 0f;

            // Local event
            m_OnPickedObjectUp.Invoke(carryTarget);
        }

        protected override void OnObjectDropped()
        {
            // Allow collisions between character collider and carried object colliders
            if (m_CharacterCollider != null)
            {
                for (int i = 0; i < m_Colliders.Count; ++i)
                {
                    if (m_Colliders[i] != null)
                        Physics.IgnoreCollision(m_CharacterCollider, m_Colliders[i], false);
                }
            }

            // Reset carry target rigidbody
            if (carryTarget != null)
            {
                carryTarget.interpolation = m_OldInterpolation;
                carryTarget.useGravity = m_OldGravity;
                carryTarget.ResetInertiaTensor();

                // Remove from character controller ignore list
                m_CharacterController.ResetIgnoreRigidbody(carryTarget);

                // Local event
                m_OnDroppedObject.Invoke(carryTarget);
            }
        }

        Vector3 GetTargetWorldCenter()
        {
            return carryTarget.position + carryTarget.rotation * GetOffset();
        }

        protected override void TickCarryPhysics()
        {
            // Blend anchor position and rotation to targets
            m_Anchor.localPosition = Vector3.Lerp(m_Anchor.localPosition, new Vector3(0f, 0f, m_CurrentAnchorZ), Time.deltaTime * 5f);
            m_Anchor.localRotation = Quaternion.Lerp(m_Anchor.localRotation, m_Rotation, Time.deltaTime * 5f);

            // Get the position delta from object center of mass to anchor position
            Vector3 deltaPos = m_Anchor.position - GetTargetWorldCenter();

            // Check if out of break range and increment timer if so
            if (deltaPos.sqrMagnitude > (m_BreakDistance * m_BreakDistance))
                m_BreakTimer += Time.deltaTime;
            else
                m_BreakTimer = 0f;

            // If out of range for too long, drop object
            if (m_BreakTimer >= m_BreakDuration)
            {
                DropObject();
            }
            else
            {
                // Get the target velocity force to match positions
                Vector3 targetVelocity = Vector3.ClampMagnitude(deltaPos * m_CarrySpeed, m_CarrySpeed) * m_PositionBlend;
				#if UNITY_6000_0_OR_NEWER
                carryTarget.AddForce(targetVelocity - carryTarget.linearVelocity, ForceMode.VelocityChange);
				#else
                carryTarget.AddForce(targetVelocity - carryTarget.velocity, ForceMode.VelocityChange);
				#endif

                // Calculate torque to rotate to target
                // Based on the following answers thread:
                // https://answers.unity.com/questions/48836/determining-the-torque-needed-to-rotate-an-object.html
                var x = Vector3.Cross(carryTarget.transform.up, m_Anchor.up);
                float theta = Mathf.Asin(x.magnitude);
                var w = x.normalized * theta / Time.deltaTime;
                var q = carryTarget.rotation * carryTarget.inertiaTensorRotation;
                var t = q * Vector3.Scale(carryTarget.inertiaTensor, Quaternion.Inverse(q) * w);
                carryTarget.AddTorque((t - carryTarget.angularVelocity), ForceMode.Impulse);

                // Calculate y-axis torque
                // Could probably be merged into the above, but I'm too dumb
                float twist = Vector3.SignedAngle(carryTarget.transform.forward, m_Anchor.forward, carryTarget.transform.up);
                carryTarget.AddRelativeTorque(0f, twist * Time.deltaTime * 10f, 0f, ForceMode.Impulse);
            }
        }

        protected override void OnManipulateObject(Vector2 mouseDelta, Vector2 analogue)
        {
            // Get mouse & analog horizontal
            float rotateX = (mouseDelta.x * FpsSettings.input.horizontalMouseSensitivity * m_MouseRotateRate) + (analogue.x * m_AnalogRotateRate * Time.deltaTime);

            // Get mouse pitch
            float mouseY = mouseDelta.y * FpsSettings.input.verticalMouseSensitivity * m_MouseRotateRate;
            if (FpsSettings.input.invertMouse)
                mouseY *= -1f;

            // Get analogue pitch
            float analogueY = analogue.y * m_AnalogRotateRate * Time.deltaTime;
            if (FpsSettings.gamepad.invertLook)
                analogueY *= -1f;

            // Combined pitch
            float rotateY = mouseY + analogueY;

            // Rotate
            m_Rotation = Quaternion.Euler(-rotateY, rotateX, 0f) * m_Rotation;
        }

        protected override void OnPushObject(float scroll, int directionInput)
        {
            float moveSpeed = scroll * m_PushScrollMultiplier + directionInput * m_PushDirectionalSpeed;

            m_CurrentAnchorZ = Mathf.Clamp(m_CurrentAnchorZ + moveSpeed * Time.deltaTime, -m_PushBackwardLimit, m_PushForwardLimit);

            /*
            [SerializeField, Min(0.01f), Tooltip("A multiplier applied to mouse scroll to determing movement speed when pushing the carried object forwards/backwards.")]
            protected float m_PushScrollMultiplier = 60f;

            [SerializeField, Min(0.05f), Tooltip("The movement speed when pushing the carried object forwards/backwards.")]
            protected float m_PushDirectionalSpeed = 0.5f;

            [SerializeField, Min(0f), Tooltip("The maximum forward distance the carried object can be pushed from its default starting position")]
            protected float m_PushForwardLimit = 0.5f;

            [SerializeField, Min(0f), Tooltip("The maximum backwards distance the carried object can be pushed from its default starting position")]
            protected float m_PushBackwardLimit = 0.5f;
            */
        }

        protected override void AddThrowForceToObject()
        {
            carryTarget.AddForce(m_ThrowDirection.forward * m_ThrowForce, m_ThrowForceMode);
        }

        protected override Quaternion GetStartingOrientation(Quaternion current)
        {
            return Quaternion.identity;
        }

        protected override bool CanManipulate()
        {
            return carryState == CarryState.Carrying;
        }

        protected override Vector3 GetOffset()
        {
            return carryTarget.centerOfMass;
        }

        static readonly NeoSerializationKey k_GravityKey = new NeoSerializationKey("gravity");
        static readonly NeoSerializationKey k_InterpolationKey = new NeoSerializationKey("interpolation");

        public override void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            base.WriteProperties(writer, nsgo, saveMode);

            writer.WriteValue(k_GravityKey, m_OldGravity);
            writer.WriteValue(k_InterpolationKey, (int)m_OldInterpolation);
        }

        public override void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            base.ReadProperties(reader, nsgo);

            if (didLoadFromSave)
            {
                reader.TryReadValue(k_GravityKey, out m_OldGravity, m_OldGravity);
                if (reader.TryReadValue(k_InterpolationKey, out int i, 0))
                    m_OldInterpolation = (RigidbodyInterpolation)i;

                carryTarget.interpolation = m_OldInterpolation;
                carryTarget.useGravity = m_OldGravity;
            }
        }
    }
}
