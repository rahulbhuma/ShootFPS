
namespace NeoCC
{
    public interface INeoCharacterControllerHitHandler
    {
        bool enabled { get; set; }

        void OnNeoCharacterControllerHit(NeoCharacterControllerHit hit);
    }
}
