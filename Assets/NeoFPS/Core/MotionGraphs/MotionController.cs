using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using NeoCC;
using NeoFPS.CharacterMotion.MotionData;
using NeoSaveGames.Serialization;
using NeoSaveGames;
using UnityEngine.Serialization;

namespace NeoFPS.CharacterMotion
{
    //[RequireComponent(typeof(NeoCharacterController))]
    [HelpURL("https://docs.neofps.com/manual/motiongraphref-mb-motioncontroller.html")]
    public class MotionController : MonoBehaviour, IMotionController, ICharacterStepTracker, INeoSerializableComponent
    {
        [Header("Motion Graph & Data")]

        [SerializeField, Tooltip("The motion graph for the controller to use (a unique instance will be instantiated from this).")]
        private MotionGraphContainer m_MotionGraph = null;

        [SerializeField, Tooltip("A set of motion data overrides for the controller to use.")]
        private MotionGraphDataOverrideAsset m_DataOverrides = null;

        [Header("Colliders")]
        
        [SerializeField, Tooltip("If this is enabled, then the collider will provide an offset that can be used to provide extra height to a jump so it appears the legs are tucked up instead of the head ducked down.")]
        private bool m_UseCrouchJump = true;

        [Header("Misc")]

        [SerializeField, Tooltip("Should the component be initialised manually or automatically in Awake and Start? Switch this on for things like networked players.")]
        private bool m_ManualInitialisation = false;

        [SerializeField, Tooltip("An optional animator component for the character body. This allows motion graph states and behaviours to easily drive animations.")]
        private Animator m_BodyAnimator = null;

        [SerializeField, FormerlySerializedAs("m_HeadRoot"), NeoObjectInHierarchyField(false, required = true), Tooltip("The root transform of the head heirarchy (height interpolated when crouching, or matched to animator hierarchy).")]
        private Transform m_UpperBodyRoot = null;

        [SerializeField, FormerlySerializedAs("m_CharacterHeadBone"), NeoObjectInHierarchyField("m_BodyAnimator", RootPropertyType.Component, false), Tooltip("A bone in the character's skeleton that's used as a target for the head root.")]
        private Transform m_UpperBodyTarget = null;

        [SerializeField, AnimatorParameterKey("m_BodyAnimator", AnimatorControllerParameterType.Float), Tooltip("The name of the float parameter in the character body's animator controller that is used to control the character height blend.")]
        private string m_HeightAnimFloat = string.Empty;

        private List<MotionGraph> m_ParentChain = new List<MotionGraph>(8);
        private List<MotionGraphConnectable> m_CyclicCheck = new List<MotionGraphConnectable>(8);
        private bool m_CyclicError = false;

        private Vector3 m_FrameMove = Vector3.zero;
        private Vector3 m_PreviousVelocity = Vector3.zero;
        private NeoCharacterCollisionFlags m_PreviousCollisionFlags = NeoCharacterCollisionFlags.None;
        private int m_HeightAnimHash = 0;

        private MotionGraphContainer m_GraphInstance = null;
        public MotionGraphContainer motionGraph
        {
            get
            {
                if (m_GraphInstance != null)
                    return m_GraphInstance;
                else
                    return m_MotionGraph;
            }
        }

        private MotionGraphState m_CurrentState = null;
        public MotionGraphState currentState
        {
            get { return m_CurrentState; }
            private set
            {
                if (value != null && m_CurrentState != value)
                {
                    // Get shared parent between old and new state
                    MotionGraph commonParent = MotionGraphConnectable.GetSharedParent(m_CurrentState, value);

                    // Exit old state and parents up to shared
                    MotionGraphConnectable connectable = m_CurrentState;
                    while (connectable != commonParent)
                    {
                        connectable.OnExit();
                        connectable = connectable.parent;
                    }

                    // Set the new state
                    m_CurrentState = value;

                    // Enter new state and parents up to shared
                    connectable = m_CurrentState;
                    while (connectable != commonParent)
                    {
                        connectable.OnEnter();
                        connectable = connectable.parent;
                    }

                    // Fire event
                    m_OnMotionGraphStateChange.Invoke();
                }
            }
        }

        public event UnityAction onCurrentStateChanged
        {
            add { m_OnMotionGraphStateChange.AddListener(value); }
            remove { m_OnMotionGraphStateChange.RemoveListener(value); }
        }

        public bool isLocal
        {
            get;
            private set;
        }

        public Transform localTransform
        {
            get;
            private set;
        }

        public Animator bodyAnimator
        {
            get { return m_BodyAnimator; }
        }

        public INeoCharacterController characterController
        {
            get;
            private set;
        }

        public IAimController aimController
        {
            get;
            private set;
        }

        public IRootMotionHandler rootMotionHandler
        {
            get;
            private set;
        }

        public Vector2 inputMoveDirection
        {
            get;
            set;
        }

        public float inputMoveScale
        {
            get;
            set;
        }

        #region GROUNDING & CONTACTS

        public float groundSlopeAngle
        {
            get;
            private set;
        }

        public Vector3 groundDownSlopeVector
        {
            get;
            private set;
        }

        public Vector3 groundAcrossSlopeVector
        {
            get;
            private set;
        }

        void CalculateSlopeEffect(bool grounded)
        {
            // Skip if airborne
            if (!grounded)
            {
                groundSlopeAngle = 0f;
                groundDownSlopeVector = Vector3.zero;
                groundAcrossSlopeVector = Vector3.zero;
            }
            else
            {
                // Angle from vertical
                groundSlopeAngle = Mathf.Acos(characterController.groundNormal.y) * Mathf.Rad2Deg;

                // Get the down-slope direction vector
                if (groundSlopeAngle > 1f)
                {
                    groundDownSlopeVector = Vector3.ProjectOnPlane(Vector3.down, characterController.groundNormal).normalized;
                    groundAcrossSlopeVector = Vector3.Cross(characterController.groundNormal, groundDownSlopeVector);
                }
                else
                {
                    groundDownSlopeVector = Vector3.zero;
                    groundAcrossSlopeVector = Vector3.zero;
                }
            }
        }

        #endregion

        #region FORCES

        [Header("Force Events")]

        [SerializeField, Tooltip("This event is called whenever the controller graph state changes (does not include states that are immediately transitioned out of).")]
        private UnityEvent m_OnMotionGraphStateChange = new UnityEvent();

        [SerializeField, Tooltip("This event is called when the controller first contacts the ground after being airborne (parameters = impulse(Vector3), mass(float))")]
        private ImpulseEvent m_OnGroundImpact = new ImpulseEvent();

        [SerializeField, Tooltip("This event is called whenever the top of the controller capsule makes initial contact with a collider (parameters = impulse(Vector3), mass(float))")]
        private ImpulseEvent m_OnHeadImpact = new ImpulseEvent();

        [SerializeField, Tooltip("This event is called whenever the sides of the controller capsule makes initial contact with a collider (parameters = impulse(Vector3), mass(float))")]
        private ImpulseEvent m_OnBodyImpact = new ImpulseEvent();

        [Serializable]
        public class ImpulseEvent : UnityEvent<Vector3> { }

        public event UnityAction<Vector3> onGroundImpact
        {
            add { m_OnGroundImpact.AddListener(value); }
            remove { m_OnGroundImpact.RemoveListener(value); }
        }
        public event UnityAction<Vector3> onHeadImpact
        {
            add { m_OnHeadImpact.AddListener(value); }
            remove { m_OnHeadImpact.RemoveListener(value); }
        }
        public event UnityAction<Vector3> onBodyImpact
        {
            add { m_OnBodyImpact.AddListener(value); }
            remove { m_OnBodyImpact.RemoveListener(value); }
        }

        #endregion

        #region UPDATE

        Vector3 m_HeadHitNormal = Vector3.zero;
        Vector3 m_BodyHitNormal = Vector3.zero;
        float m_HeadHitAvgSum = 0f;
        int m_HeadHitCount = 0;
        float m_BodyHitAvgSum = 0f;
        int m_BodyHitCount = 0;
        bool m_StickToGround = false;
        bool m_ApplyGravity = false;

        void UpdateConnectable(MotionGraphConnectable connectable)
        {
            if (connectable.parent != null)
                UpdateConnectable(connectable.parent);
            connectable.Update();
        }

        void DynamicUpdateConnectable(MotionGraphConnectable connectable)
        {
            if (connectable.parent != null)
                DynamicUpdateConnectable(connectable.parent);
            connectable.DynamicUpdate();
        }

        void GetMoveVector(out Vector3 move, out bool applyGravity, out bool stickToGround)
        {
            // Skip if time is effectively paused
            if (Mathf.Approximately(Time.timeScale, 0.0f))
            {
                move = Vector3.zero;
                applyGravity = m_ApplyGravity = false;
                stickToGround = m_StickToGround = false;
                return;
            }

            // Get the next state
            if (currentState != null)
            {
                currentState = GetNextState(currentState);
                UpdateConnectable(currentState);
                m_FrameMove = currentState.moveVector;

                // Check for root motion
                if (rootMotionHandler != null && rootMotionStrength > 0.001f)
                {
                    // Get position vector
                    Vector3 positionOffset = rootMotionHandler.GetRootMotionPositionOffset();

                    // Apply damping to root motion results
                    if (rootMotionDamping > 0f)
                    {
                        if (m_FirstRootMotionFrame)
                        {
                            m_RootMotionPosition = positionOffset;
                            m_RootMotionVelocity = Vector3.zero;
                            m_FirstRootMotionFrame = false;
                        }
                        else
                        {
                            m_RootMotionPosition = Vector3.SmoothDamp(m_RootMotionPosition, positionOffset, ref m_RootMotionVelocity, Mathf.Lerp(0f, 0.25f, m_RootMotionDamping));
                            positionOffset = m_RootMotionPosition;
                        }
                    }

                    // Lerp movement
                    m_FrameMove = Vector3.Lerp(m_FrameMove, positionOffset * rootMotionPositionMultiplier, rootMotionStrength);

                    // Add root yaw
                    if (!Mathf.Approximately(rootMotionRotationMultiplier, 0f))
                    {
                        var rootRotation = rootMotionHandler.GetRootMotionRotationOffset();
                        if (rootRotation != Quaternion.identity)
                        {
                            // Scale
                            if (!Mathf.Approximately(rootMotionRotationMultiplier, 1f))
                                rootRotation = Quaternion.SlerpUnclamped(Quaternion.identity, rootRotation, rootMotionRotationMultiplier);

                            var up = localTransform.up;
                            var charForwards = localTransform.forward;
                            var rootForwards = Vector3.ProjectOnPlane(rootRotation * localTransform.forward, up);
                            characterController.AddYawOffset(Vector3.SignedAngle(charForwards, rootForwards, up));
                        }
                    }
                }
                else
                    m_FirstRootMotionFrame = true;

                // Apply gravity / grounding force
                m_ApplyGravity = currentState.applyGravity;
                m_StickToGround = currentState.applyGroundingForce;
            }
            else
            {
                // Only outside influences
                m_ApplyGravity = true;
                m_StickToGround = true;
            }

            // Reset checked triggers
            motionGraph.ResetCheckedTriggers();

            // Reset the impact tracker
            ResetHits();

            // Set the frame move
            move = m_FrameMove;
            stickToGround = m_StickToGround;
            applyGravity = m_ApplyGravity;
        }

        void ResetHits()
        {
            m_HeadHitNormal = Vector3.zero;
            m_BodyHitNormal = Vector3.zero;
            m_HeadHitAvgSum = 0f;
            m_HeadHitCount = 0;
            m_BodyHitAvgSum = 0f;
            m_BodyHitCount = 0;
        }

        void OnMoved()
        {
            var flags = characterController.collisionFlags;

            // Trigger ground impact event
            if ((flags & NeoCharacterCollisionFlags.Below) == NeoCharacterCollisionFlags.Below &&
                (m_PreviousCollisionFlags & NeoCharacterCollisionFlags.Below) == NeoCharacterCollisionFlags.None)
            {
                m_OnGroundImpact.Invoke(characterController.groundNormal * -Vector3.Dot(m_PreviousVelocity, characterController.groundNormal));
            }

            if (m_HeadHitCount > 0)
            {
                m_HeadHitNormal /= m_HeadHitAvgSum;
                m_HeadHitNormal.Normalize();
                m_OnHeadImpact.Invoke(m_HeadHitNormal * -Vector3.Dot(m_PreviousVelocity, m_HeadHitNormal));
            }

            if (m_BodyHitCount > 0)
            {
                m_BodyHitNormal /= m_BodyHitAvgSum;
                m_BodyHitNormal.Normalize();
                m_OnBodyImpact.Invoke(m_BodyHitNormal * -Vector3.Dot(m_PreviousVelocity, m_BodyHitNormal));
            }

            m_PreviousVelocity = characterController.velocity;
            m_PreviousCollisionFlags = flags;

#if UNITY_EDITOR
            TickDebugger(m_FrameMove, m_ApplyGravity, m_StickToGround);
#endif
        }

        void OnControllerColliderHit(NeoCharacterControllerHit hit)
        {
            // Record head hits if none last frame
            if (hit.collisionFlags == NeoCharacterCollisionFlags.Above && (m_PreviousCollisionFlags & NeoCharacterCollisionFlags.Above) == NeoCharacterCollisionFlags.None)
            {
                ++m_HeadHitCount;
                float dot = Vector3.Dot(hit.normal, characterController.up);
                m_HeadHitNormal += hit.normal * dot;
                m_HeadHitAvgSum += dot;
            }

            // Record body hits if none last frame
            if ((hit.collisionFlags & NeoCharacterCollisionFlags.Sides) == NeoCharacterCollisionFlags.Sides &&
                (m_PreviousCollisionFlags & NeoCharacterCollisionFlags.Sides) == NeoCharacterCollisionFlags.None)
            {
                ++m_BodyHitCount;
                ++m_BodyHitAvgSum;
                m_BodyHitNormal += hit.normal;
            }
        }

        #endregion

        #region INITIALISATION

        private bool m_InitialisedGraph = false;
        private bool m_Initialised = false;

        private static List<SkinnedMeshRenderer> s_SkinnedMeshes = new List<SkinnedMeshRenderer>();

        protected virtual void Awake()
        {
            localTransform = transform;
            aimController = GetComponent<IAimController>();
            characterController = GetComponent<INeoCharacterController>();
            characterController.Initialise();
            characterController.SetMoveCallback(GetMoveVector, OnMoved);
            characterController.onHeightChanged += OnHeightChanged;
            characterController.onControllerHit += OnControllerColliderHit;

            if (m_BodyAnimator != null)
            {
                m_UseCrouchJump = false; // Can't crounch jump with an animated character (yet)
                 
                if (!string.IsNullOrWhiteSpace(m_HeightAnimFloat))
                    m_HeightAnimHash = Animator.StringToHash(m_HeightAnimFloat);

                rootMotionHandler = bodyAnimator.GetComponent<IRootMotionHandler>();
                rootMotionPositionMultiplier = 1f;
                rootMotionRotationMultiplier = 1f;
                rootMotionDamping = 0.25f;

                // Set animator and skinned meshes to always update to prevent culling issues when looking up
                m_BodyAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                m_BodyAnimator.GetComponentsInChildren(true, s_SkinnedMeshes);
                for (int i = 0; i < s_SkinnedMeshes.Count; ++i)
                    s_SkinnedMeshes[i].updateWhenOffscreen = true;
                s_SkinnedMeshes.Clear();
            }

            InitialiseGraph();

            InitialiseUpperBody();

            if (!m_ManualInitialisation)
                ManualInitialise(true);
        }

        void InitialiseGraph()
        {
            if (!m_InitialisedGraph)
            {
                // Future work: If this is the editor, and the player character, do not make unique
                // This allows for changes to graph and data settings to be properly serialized
                // while editing. Find a clean way to do this
                if (m_MotionGraph != null)
                    m_GraphInstance = m_MotionGraph.DeepCopy();

                if (motionGraph != null)
                {
                    motionGraph.Initialise(this);
                    currentState = GetNextState(motionGraph.rootNode);
                    if (m_DataOverrides != null)
                        motionGraph.AddDataOverrides(m_DataOverrides);
                }

                m_InitialisedGraph = true;
            }
        }

        public void ManualInitialise(bool local)
        {
            // Prevent double initialisation
            if (m_Initialised)
            {
#if UNITY_EDITOR
                Debug.LogError("Attempting to initialise character mover multiple times. Make sure manual initialisation is toggled on if you want to use this.");
#endif
                return;
            }
            m_Initialised = true;
            
            // Set local (blocks calculations conflicting between server and client if relevant)
            isLocal = local;
        }

        protected virtual void Start()
        {
        }

        #endregion

        #region COLLIDER

        enum HeightState
        {
            Stable,
            SizingDown,
            SizingUp
        }

        private HeightState m_HeightState = HeightState.Stable;
        private float m_HeightChangeRate = 1f;
        private float m_StandingHeight = 0f;
        private float m_TargetHeight = 0f;
        private float m_UpperBodyOffset = 0f;

        public float currentHeight
        {
            get
            {
                if (m_HeightAnimHash == 0)
                    return m_UpperBodyRoot.localPosition.y + m_UpperBodyOffset;
                else
                    return m_BodyAnimator.GetFloat(m_HeightAnimHash) * m_StandingHeight;
            }
            private set
            {
                if (m_HeightAnimHash == 0)
                    m_UpperBodyRoot.localPosition = new Vector3(0f, value - m_UpperBodyOffset, 0f);
                else
                    m_BodyAnimator.SetFloat(m_HeightAnimHash, value / m_StandingHeight);
            }
        }

        public float currentHeightNormalised
        {
            get { return currentHeight / m_StandingHeight; }
        }

        void InitialiseUpperBody()
        {
            // Get upper body height
            if (m_UpperBodyRoot != null)
            {
                m_StandingHeight = m_TargetHeight = characterController.height;
                m_UpperBodyOffset = m_StandingHeight - m_UpperBodyRoot.localPosition.y;
            }
        }

        public float GetHeightMultiplier()
        {
            return m_TargetHeight / m_StandingHeight;
        }

        public void SetHeightMultiplier(float multiplier, float duration, CharacterResizePoint point = CharacterResizePoint.Automatic)
        {
            if (m_HeightState == HeightState.SizingUp)
                characterController.CancelHeightChange();

            // Get height change rate
            if (duration > 0.01f)
            {
                m_HeightChangeRate = 1f / duration;// (duration * diff);
            }
            else
                m_HeightChangeRate = 100f;

            float h = m_StandingHeight * multiplier;
            if (h < currentHeight) // Should it be current, target, or controller?
            {
                switch (point)
                {
                    case CharacterResizePoint.Automatic:
                        if (characterController.isGrounded || !m_UseCrouchJump)
                        {
                            // Wait for head interpolation and then crouch from base
                            m_TargetHeight = h;
                            m_HeightState = HeightState.SizingDown;
                        }
                        else
                        {
                            // Crouch from top (crouch jump). Skip sizing down state as head doesn't move.
                            characterController.SetHeight(h, 1f);
                        }
                        break;
                    case CharacterResizePoint.Bottom:
                        // Wait for head interpolation and then crouch from base
                        m_TargetHeight = h;
                        m_HeightState = HeightState.SizingDown;
                        break;
                    case CharacterResizePoint.Top:
                        // Crouch from top (crouch jump). Skip sizing down state as head doesn't move.
                        characterController.SetHeight(h, 1f);
                        break;
                }
            }
            else
            {
                // Enter sizing up state as waiting for space to change height
                m_HeightState = HeightState.SizingUp;

                switch (point)
                {
                    case CharacterResizePoint.Automatic:
                        // Stand from bottom or top depending on grounding
                        if (characterController.isGrounded || !m_UseCrouchJump)
                            characterController.SetHeight(h, 0f);
                        else
                            characterController.SetHeight(h, 1f);
                        break;
                    case CharacterResizePoint.Bottom:
                        characterController.SetHeight(h, 0f);
                        break;
                    case CharacterResizePoint.Top:
                        characterController.SetHeight(h, 1f);
                        break;
                }
            }
        }

        public bool CheckIsHeightMultiplierRestricted(float multiplier)
        {
            return characterController.IsHeightRestricted(Mathf.Clamp01(multiplier) * m_StandingHeight);
        }

        public void ChangeStandingHeight(float height)
        {
            height = Mathf.Max(height, characterController.radius * 2f);
            float oldHeightMultiplier = GetHeightMultiplier();

            m_StandingHeight = height;

            SetHeightMultiplier(oldHeightMultiplier, 0.5f);
        }

        void OnHeightChanged(float newHeight, float rootOffset)
        {
            // Move the upper body to compensate for the root transform moving
            m_UpperBodyRoot.localPosition += new Vector3(0f, -rootOffset, 0f);

            m_TargetHeight = newHeight;
            m_HeightState = HeightState.Stable;
        }

        private const float k_HeadBoundsWidth = 0.05f;

        protected void Update()
        {
            // Dynamic update for connectable
            if (currentState != null)
                DynamicUpdateConnectable(currentState);

            TrackSteps();

            // Position head root to match character skeleton
            if (m_UpperBodyRoot != null && m_UpperBodyTarget != null)
            {
                // Get root position & relative head position
                var rootPosition = localTransform.position;
                var headPosition = m_UpperBodyTarget.position - rootPosition;

                // Get height of head
                var headHeight = Vector3.Dot(headPosition, localTransform.up);

                // clamp head position on root plane and convert to world pos
                headPosition -= localTransform.up * headHeight;
                headPosition = Vector3.ClampMagnitude(headPosition, characterController.radius - k_HeadBoundsWidth);
                headPosition += localTransform.up * headHeight + rootPosition;

                // Apply to head
                m_UpperBodyRoot.position = headPosition;
            }

            if (!Mathf.Approximately(currentHeight, m_TargetHeight))
            {
                if (currentHeight < m_TargetHeight)
                {
                    // Move head up
                    float to = currentHeight + m_HeightChangeRate * Time.deltaTime;
                    if (to > m_TargetHeight)
                        to = m_TargetHeight;

                    currentHeight = to;
                }
                else
                {
                    // Move head down
                    float to = currentHeight - m_HeightChangeRate * Time.deltaTime;
                    if (to < m_TargetHeight)
                    {
                        to = m_TargetHeight;
                        if (m_HeightState == HeightState.SizingDown)
                        {
                            // In sizing down state, waits for head to lower before resizing capsule to
                            // prevent head clipping through low obstacles at start of crouch
                            characterController.SetHeight(m_TargetHeight, 0f);
                        }
                    }

                    currentHeight = to;

                    // Add grounded check during sizing up, so if it loses grounding, switch to
                    // crouch jump style duck and calculate normalised height for origin??
                }
            }

            BlendRootMotion();
        }

        #endregion

        #region ROOT MOTION

        private float m_RootMotionTarget = 0f;
        private float m_RootMotionRate = 1f;
        private float m_RootMotionDamping = 0f;
        private Vector3 m_RootMotionPosition = Vector3.zero;
        private Vector3 m_RootMotionVelocity = Vector3.zero;
        private bool m_FirstRootMotionFrame = true;

        public float rootMotionStrength
        {
            get;
            private set;
        }

        public float rootMotionPositionMultiplier
        {
            get;
            set;
        }

        public float rootMotionRotationMultiplier 
        {
            get;
            set;
        }

        public float rootMotionDamping
        {
            get { return m_RootMotionDamping; }
            set { m_RootMotionDamping = Mathf.Clamp01(value); }
        }

        public void SetRootMotionStrength(float strength, float blendTime)
        {
            if (rootMotionHandler != null)
            {
                // Clamp strength
                strength = Mathf.Clamp01(strength);

                m_RootMotionTarget = strength;
                if (blendTime > 0.001f)
                    m_RootMotionRate = 1f / blendTime;
                else
                    rootMotionStrength = m_RootMotionTarget;
            }
        }

        void BlendRootMotion()
        {
            if (rootMotionStrength < m_RootMotionTarget - 0.0001f)
            {
                rootMotionStrength += Time.deltaTime * m_RootMotionRate;
                if (rootMotionStrength > m_RootMotionTarget)
                    rootMotionStrength = m_RootMotionTarget;
            }

            if (rootMotionStrength > m_RootMotionTarget + 0.0001f)
            {
                rootMotionStrength -= Time.deltaTime * m_RootMotionRate;
                if (rootMotionStrength < m_RootMotionTarget)
                    rootMotionStrength = m_RootMotionTarget;
            }
        }

        #endregion

        #region MOTION GRAPH

        MotionGraphState GetNextState(MotionGraphConnectable c)
        {
            m_CyclicCheck.Clear();
            MotionGraphState result = CheckParents(c);
            if (result != null)
                return result;

            return GetNextStateInternal(c);
        }

        MotionGraphState CheckParents(MotionGraphConnectable c)
        {
            if (c == null)
                return null;

            // Get parent chain (we want to start from root)
            m_ParentChain.Clear();
            while (c.parent != null)
            {
                m_ParentChain.Add(c.parent);
                c = c.parent;
            }

            // Iterate through parents from root to current
            // Skip the absolute root, as we only want transitions OUT of the
            // Sub-graph, which the root cannot have
            for (int p = m_ParentChain.Count - 2; p >= 0; --p)
            {
                MotionGraph parent = m_ParentChain[p];
                for (int i = 0; i < parent.connections.Count; ++i)
                {
                    MotionGraphConnection transition = parent.connections[i];
                    if (transition.destination.parent != parent)
                    {
                        if (transition.CheckConditions())
                        {
                            return GetNextStateInternal(transition.destination);
                        }
                    }
                }
            }

            return null;
        }

        MotionGraphState GetNextStateInternal(MotionGraphConnectable c)
        {
            if (m_CyclicCheck.Contains(c))
            {
                if (!m_CyclicError)
                {
                    Debug.Log("Cyclic transition detected in motion graph. Using starting state", this);
                    string sequence = "sequence: ";// + c.name;
                    for (int i = 0; i < m_CyclicCheck.Count; ++i)
                    {
                        if (i > 0)
                            sequence += ", ";
                        sequence += m_CyclicCheck[i].name;
                    }
                    sequence += ", " + c.name;
                    Debug.Log(sequence);
                    m_CyclicError = true;
                }
                return null;
            }
            else
                m_CyclicCheck.Add(c);

            // Check connectable transitions
            for (int i = 0; i < c.connections.Count; ++i)
            {
                MotionGraphConnection transition = c.connections[i];
                if (transition.destination.CheckCanEnter() && transition.CheckConditions())
                    return GetNextStateInternal(transition.destination);
            }

            // No transitions. If it's a graph, return default. If state, return this
            MotionGraph graph = c as MotionGraph;
            if (graph != null)
            {
                if (graph.defaultEntry != null)
                    return GetNextStateInternal(graph.defaultEntry);
                else
                    return null;
            }
            else
                return c as MotionGraphState;
        }

        #endregion

        #region STEP TRACKING

        [Header("Step Tracking")]

        [SerializeField, Tooltip("Switch this to true if you aren't tracking steps in the motion graph, and they will simply be counted at a default rate whenever the character is grounded.")]
        private bool m_UseDumbStepping = false;

        public event UnityAction onStep;

        private float m_StrideLength = 0f;
        private float m_MaxStepRate = 2f;

        public float smoothedStepRate
        {
            get;
            private set;
        }

        public float stepCounter
        {
            get;
            private set;
        }

        public float strideLength
        {
            get { return m_StrideLength; }
            set
            {
                if (value < 0.001f)
                    m_StrideLength = 0f;
                else
                    m_StrideLength = Mathf.Clamp(value, 0.2f, 50f);
            }
        }

        public float maxStepRate
        {
            get { return m_MaxStepRate; }
            set { m_MaxStepRate = Mathf.Clamp(value, 0.2f, 5f); }
        }

        public void SetWholeStep()
        {
            stepCounter = Mathf.Ceil(stepCounter);
        }

        void TrackSteps()
        {
            // Get the stride length (if using dumb stepping, steps are counted at a strideLength of 3m when grounded)
            float sl = strideLength;
            if (m_UseDumbStepping)
                sl = characterController.isGrounded ? 3f : 0f;

            if (sl > 0f)
            {
                // Get a smoothed version of the speed value (prevents jitter when changing direction)
                smoothedStepRate = Mathf.Lerp(smoothedStepRate, Mathf.Min(characterController.velocity.magnitude / sl, maxStepRate * 2f), Time.deltaTime * 5f);

                if (onStep != null)
                {
                    // Record old step count (floor)
                    float oldStepCounter = Mathf.Floor(stepCounter);

                    // Increment step counter
                    stepCounter += smoothedStepRate * Time.deltaTime;

                    // If whole number has increased, fire event
                    if (Mathf.Floor(stepCounter) > oldStepCounter)
                        onStep();
                }
                else
                    stepCounter += smoothedStepRate * Time.deltaTime;
            }
            else
            {
                // Fade out smoothedStepRate (so that it starts at zero when steps are next enabled instead of popping)
                smoothedStepRate = Mathf.Lerp(smoothedStepRate, 0f, Time.deltaTime);
                if (smoothedStepRate < 0.0001f)
                    smoothedStepRate = 0f;
                stepCounter += smoothedStepRate * Time.deltaTime;
            }
        }

        #endregion

        #region SERIALIZATION

        private static readonly NeoSerializationKey k_CurrentStateKey = new NeoSerializationKey("currentState");

        public void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            if (saveMode == SaveMode.Default)
            {
                // Save graph
                writer.PushContext(SerializationContext.ObjectNeoSerialized, 0);
                m_GraphInstance.WriteProperties(writer);
                writer.PopContext(SerializationContext.ObjectNeoSerialized);

                // Save current state
                writer.WriteValue(k_CurrentStateKey, currentState.serializationKey);
                writer.PushContext(SerializationContext.ObjectNeoSerialized, currentState.serializationKey);
                currentState.WriteProperties(writer);
                writer.PopContext(SerializationContext.ObjectNeoSerialized);
            }
        }

        public void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            InitialiseGraph();

            // Load graph
            if (m_GraphInstance != null)
            {
                // Load current state
                int key;
                if (reader.TryReadValue(k_CurrentStateKey, out key, 0))
                {
                    var found = m_GraphInstance.GetStateFromKey(key);
                    if (found != null)
                    {
                        currentState = found;
                        if (reader.PushContext(SerializationContext.ObjectNeoSerialized, key))
                        {
                            try
                            {
                                currentState.ReadProperties(reader);
                            }
                            finally
                            {
                                reader.PopContext(SerializationContext.ObjectNeoSerialized, key);
                            }
                        }
                    }
                }

                if (reader.PushContext(SerializationContext.ObjectNeoSerialized, 0))
                {
                    try
                    {
                        m_GraphInstance.ReadProperties(reader);
                    }
                    finally
                    {
                        reader.PopContext(SerializationContext.ObjectNeoSerialized, 0);
                    }
                }
            }
        }

        #endregion

        #region DEBUG GIZMOS
#if UNITY_EDITOR

        void OnDrawGizmos()
        {
            if (!Application.isPlaying && gameObject.scene.IsValid())
                return;

            if (characterController == null)
                return;

            float radius = characterController.radius;
            float height = characterController.height;
            Vector3 position = localTransform.position;
            Quaternion rotation = localTransform.rotation;
            Vector3 up = rotation * Vector3.up;

            // Draw capsule
            ExtendedGizmos.DrawCapsuleMarker(radius, height, position + up * (height * 0.5f), Color.white);

            // Draw input arrow
            if (inputMoveScale > 0.01f)
            {
                float angle = Vector2.SignedAngle(inputMoveDirection, Vector2.up);
                ExtendedGizmos.DrawArrowMarkerFlat(
                    position + up * height * 0.5f,
                    rotation,
                    angle,
                    inputMoveScale,
                    Color.blue
                );
            }

            // Draw velocity arrow
            var velocity = characterController.rawVelocity;
            if (velocity.sqrMagnitude > 0.001f)
            {
                ExtendedGizmos.DrawArrowMarker3D(
                    position + up * height * 0.5f,
                    velocity.normalized,
                    velocity.magnitude * 0.2f,
                    Color.cyan
                );
            }

            // Draw ground normals
            if (characterController.isGrounded)
            {
                var normal = characterController.groundNormal;
                var surfaceNormal = characterController.groundSurfaceNormal;

                // Draw ground normal
                Vector3 contactPoint = position + (up * radius) - (normal * radius);
                ExtendedGizmos.DrawArrowMarker3D(
                    contactPoint,
                    normal,
                    radius,
                    Color.magenta
                );

                // Draw ground surface normal
                ExtendedGizmos.DrawRay(contactPoint, surfaceNormal, radius, Color.green);
            }
        }

#endif
        #endregion

        #region DEBUGGER
#if UNITY_EDITOR
        
        public delegate void OnDestroyDelegate(MotionController mc);
        public delegate void OnTickDelegate(MotionController mc, Vector3 targetMove, bool applyGravity, bool snapToGround);
        
        public event OnDestroyDelegate onDestroy;
        public event OnTickDelegate onTick;

        void TickDebugger(Vector3 targetMove, bool applyGravity, bool snapToGround)
        {
            if (onTick != null)
                onTick(this, targetMove, applyGravity, snapToGround);
        }

        protected void OnDestroy()
        {
            if (onDestroy != null)
                onDestroy(this);
        }

#endif
        #endregion
    }
}