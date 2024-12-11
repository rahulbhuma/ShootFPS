#if !NEOFPS_FORCE_QUALITY && (UNITY_ANDROID || UNITY_IOS || UNITY_TIZEN || (UNITY_WSA && NETFX_CORE) || NEOFPS_FORCE_LIGHTWEIGHT)
#define NEOFPS_LIGHTWEIGHT
#endif

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoFPS.CharacterMotion.Parameters;

namespace NeoFPS.CharacterMotion.Conditions
{
    [MotionGraphElement("Parameters/Vector Type")]
    public class VectorTypeCondition : MotionGraphCondition
    {
        [SerializeField] private VectorParameter m_Property = null;
        [SerializeField] private VectorType m_What = VectorType.IsUnitLength;
        [SerializeField] private bool m_IsTrue = true;

        private float m_ComparisonValue = float.MinValue;

        public enum VectorType
        {
            IsEmpty,
            IsUnitLength,
            IsWall,
            IsFloor,
            IsFlat
        }

        public override bool CheckCondition(MotionGraphConnectable connectable)
        {
            if (m_Property != null)
            {
                bool result = false;
                switch (m_What)
                {
                    case VectorType.IsEmpty:
                        {
                            var sqrMagnitude = m_Property.value.sqrMagnitude;
                            result = sqrMagnitude < 0.00001f;
                        }
                        break;
                    case VectorType.IsUnitLength:
                        {
                            var sqrMagnitude = m_Property.value.sqrMagnitude;
                            result = sqrMagnitude > 0.9999f && sqrMagnitude < 1.00001f;
                        }
                        break;
                    case VectorType.IsWall:
                        {
                            if (m_ComparisonValue == float.MinValue)
                            {
                                float wallAngle = controller.characterController.wallAngle;
                                m_ComparisonValue = Mathf.Sin(Mathf.Deg2Rad * wallAngle);
                            }
                            result = Mathf.Abs(Vector3.Dot(m_Property.value, controller.characterController.up)) < m_ComparisonValue;
                        }
                        break;
                    case VectorType.IsFloor:
                        {
                            if (m_ComparisonValue == float.MinValue)
                            {
                                float slopeLimit = controller.characterController.slopeLimit;
                                m_ComparisonValue = Mathf.Cos(Mathf.Deg2Rad * slopeLimit);
                            }
                            result = Vector3.Dot(m_Property.value, controller.characterController.up) > m_ComparisonValue;
                        }
                        break;
                    case VectorType.IsFlat:
                        {
                            var vector = m_Property.value;
                            var dotUp = Vector3.Dot(m_Property.value, controller.characterController.up);
                            var sqrMagnitude = vector.sqrMagnitude;
                            result = dotUp > -0.00001f && dotUp < 0.00001f && sqrMagnitude > 0.9999f && sqrMagnitude < 1.00001f;
                        }
                        break;
                    default:
                        result = !m_IsTrue;
                        break;
                }

                if (m_IsTrue)
                    return result;
                else
                    return !result;
            }
            return false;
        }

        public override void CheckReferences(IMotionGraphMap map)
        {
            m_Property = map.Swap(m_Property);
        }
    }
}
