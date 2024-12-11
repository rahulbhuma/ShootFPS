using UnityEngine;
using NeoFPS.Constants;
using NeoFPS.CharacterMotion.Parameters;

namespace NeoFPS.CharacterMotion.Behaviours
{
    [MotionGraphElement("Audio/SurfaceFootstepAudio", "SurfaceFootstepAudioBehaviour")]
    [HelpURL("https://docs.neofps.com/manual/motiongraphref-mgb-surfacefootstepaudiobehaviour.html")]
    public class SurfaceFootstepAudioBehaviour : MotionGraphBehaviour, ISurfaceFootstepAudio
    {
		[SerializeField, RequiredObjectProperty, Tooltip("The surface audio library for the slide audio clips.")]
		private SurfaceAudioData m_AudioData = null;

        [SerializeField, Tooltip("The direction to cast in to get the surface type.")]
        private Direction m_CastDirection = Direction.Down;

        [SerializeField, Tooltip("The direction vector for the ray to cast when detecting surface type.")]
        private Vector3 m_CastVector = Vector3.down;

        [SerializeField, Tooltip("The transform parameter to use for the direction space. If none is selected, defaults to world.")]
        private TransformParameter m_TransformParameter = null;

        [SerializeField, Tooltip("The direction vector parameter for the ray to cast when detecting surface type. If none is selected, defaults to character down.")]
        private VectorParameter m_VectorParameter = null;

        [SerializeField, Tooltip("The speed below which no footstep audio will be played.")]
        private float m_MinimumSpeed = 0.01f;

		[SerializeField, Range (0.05f, 2f), Tooltip("The downward raycast length for the ground surface test.")]
		private float m_MaxRayDistance = 1f;

        [SerializeField, Tooltip("If persistent is true, then exiting this state or sub-graph will keep the footstep settings until they are explicitly set from elsewhere.")]
        private bool m_Persistent = false;
        
        private SurfaceFootstepAudioSystem m_FootstepSystem = null;

        public enum Direction
        {
            Down,
            InverseGroundNormal,
            LocalVector,
            WorldVector,
            WorldParameter,
            WorldParameterInverse,
            LocalParameter,
            LocalParameterInverse,
            TransformRelative
        }

        public SurfaceAudioData footstepAudio
        {
            get { return m_AudioData; }
        }

        public float minimumSpeed
        {
            get { return m_MinimumSpeed; }
        }

        public float castRange
        {
            get { return m_MaxRayDistance; }
        }

        public Vector3 castDirection
        {
            get
            {
                // Get raycast direction
                switch (m_CastDirection)
                {
                    case Direction.Down:
                        return Vector3.down;
                    case Direction.InverseGroundNormal:
                        var cc = controller.characterController;
                        if (cc.isGrounded)
                            return -cc.groundNormal;
                        else
                            return Vector3.down;
                    case Direction.LocalVector:
                        return m_CastVector;
                    case Direction.WorldVector:
                        return m_CastVector;
                    case Direction.WorldParameter:
                        if (m_VectorParameter != null)
                            return m_VectorParameter.value.normalized;
                        else
                            return -controller.characterController.up;
                    case Direction.WorldParameterInverse:
                        if (m_VectorParameter != null)
                            return -m_VectorParameter.value.normalized;
                        else
                            return -controller.characterController.up;
                    case Direction.LocalParameter:
                        if (m_VectorParameter != null)
                            return m_VectorParameter.value.normalized;
                        else
                            return Vector3.down;
                    case Direction.LocalParameterInverse:
                        if (m_VectorParameter != null)
                            return -m_VectorParameter.value.normalized;
                        else
                            return Vector3.down;
                    case Direction.TransformRelative:
                        if (m_TransformParameter != null && m_TransformParameter.value != null)
                            return m_TransformParameter.value.rotation * m_CastVector.normalized;
                        else
                            return m_CastVector.normalized;
                    default:
                        return Vector3.down;
                }
            }
        }

        public Space castSpace
        {
            get
            {
                switch (m_CastDirection)
                {
                    case Direction.Down:
                        return Space.Self;
                    case Direction.InverseGroundNormal:
                        if (controller.characterController.isGrounded)
                            return Space.World;
                        else
                            return Space.Self;
                    case Direction.LocalVector:
                        return Space.Self;
                    case Direction.LocalParameter:
                        return Space.Self;
                    case Direction.LocalParameterInverse:
                        return Space.Self;
                    default:
                        return Space.World;
                }
            }
        }

        public override void OnValidate()
        {
            // Check minimum speed
            if (m_MinimumSpeed < 0f)
                m_MinimumSpeed = 0f;
        }

        public override void Initialise(MotionGraphConnectable o)
        {
            base.Initialise(o);

            // Get audio handler
            m_FootstepSystem = controller.GetComponent<SurfaceFootstepAudioSystem>();
            if (m_FootstepSystem == null)
                enabled = false;
        }

        public override void OnEnter()
        {
            m_FootstepSystem.SetFootstepAudio(this);
        }

        public override void OnExit()
        {
            if (!m_Persistent)
                m_FootstepSystem.SetFootstepAudio(null);
        }

        public override void CheckReferences(IMotionGraphMap map)
        {
            m_VectorParameter = map.Swap(m_VectorParameter);
            m_TransformParameter = map.Swap(m_TransformParameter);
            base.CheckReferences(map);
        }
    }
}