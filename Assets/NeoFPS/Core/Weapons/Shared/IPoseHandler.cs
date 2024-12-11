using UnityEngine;

namespace NeoFPS
{
    public interface IPoseHandler
    {
        void PushPose(PoseInformation pose, MonoBehaviour owner, float blendTime, int priority = 0);
        void PopPose(MonoBehaviour owner, float blendTime);
        PoseInformation GetPose(MonoBehaviour owner);
    }

    public class PoseInformation
    {
        public Vector3 position;
        public Quaternion rotation;
        public VectorInterpolationMethod interpolatePositionIn;
        public QuaternionInterpolationMethod interpolateRotationIn;
        public VectorInterpolationMethod interpolatePositionOut;
        public QuaternionInterpolationMethod interpolateRotationOut;

        public PoseInformation (Vector3 pos, Quaternion rot)
        {
            position = pos;
            rotation = rot;
            interpolatePositionIn = Vector3.Lerp;
            interpolateRotationIn = Quaternion.Lerp;
            interpolatePositionOut = Vector3.Lerp;
            interpolateRotationOut = Quaternion.Lerp;
        }

        public PoseInformation(Vector3 pos, Quaternion rot, VectorInterpolationMethod posInterp, QuaternionInterpolationMethod rotInterp)
        {
            position = pos;
            rotation = rot;

            if (posInterp == null)
                posInterp = Vector3.Lerp;
            if (rotInterp == null)
                rotInterp = Quaternion.Lerp;

            interpolatePositionIn = posInterp;
            interpolateRotationIn = rotInterp;
            interpolatePositionOut = posInterp;
            interpolateRotationOut = rotInterp;
        }

        public PoseInformation(Vector3 pos, Quaternion rot, VectorInterpolationMethod posInterpIn, QuaternionInterpolationMethod rotInterpIn, VectorInterpolationMethod posInterpOut, QuaternionInterpolationMethod rotInterpOut)
        {
            position = pos;
            rotation = rot;

            if (interpolatePositionIn == null)
                interpolatePositionIn = Vector3.Lerp;
            if (interpolateRotationIn == null)
                interpolateRotationIn = Quaternion.Lerp;
            if (interpolatePositionOut == null)
                interpolatePositionOut = Vector3.Lerp;
            if (interpolateRotationOut == null)
                interpolateRotationOut = Quaternion.Lerp;

            interpolatePositionIn = posInterpIn;
            interpolateRotationIn = rotInterpIn;
            interpolatePositionOut = posInterpOut;
            interpolateRotationOut = rotInterpOut;
        }
    }

    public delegate Vector3 VectorInterpolationMethod(Vector3 from, Vector3 to, float lerp);
    public delegate Quaternion QuaternionInterpolationMethod(Quaternion from, Quaternion to, float lerp);
}
