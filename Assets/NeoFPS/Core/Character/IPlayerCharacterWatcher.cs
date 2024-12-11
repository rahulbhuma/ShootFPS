
namespace NeoFPS
{
    public interface IPlayerCharacterWatcher
    {
        void AttachSubscriber(IPlayerCharacterSubscriber subscriber);
        void ReleaseSubscriber(IPlayerCharacterSubscriber subscriber);
    }
}
