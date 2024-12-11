
namespace NeoFPS
{
    public interface ILaserPointerWatcher
    {
        void RegisterLaserPointer(ILaserPointer laserPointer);
        void UnregisterLaserPointer(ILaserPointer laserPointer);
    }
}
