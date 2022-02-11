namespace fiskaltrust.Middleware.Abstractions
{
    public interface IHelper
    {
        void StartBegin();

        void StartEnd();

        void StopBegin();

        void StopEnd();

        void Message(string jsonMessage);
    }
}
