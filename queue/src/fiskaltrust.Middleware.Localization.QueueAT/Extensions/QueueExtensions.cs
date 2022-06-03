using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueAT.Extensions
{
    public static class QueueExtensions
    {
        public static bool IsActive(this ftQueue queue) => queue.StartMoment.HasValue && !queue.StopMoment.HasValue;

        public static bool IsNew(this ftQueue queue) => !queue.StartMoment.HasValue && !queue.StopMoment.HasValue;

        public static bool IsDeactivated(this ftQueue queue) => queue.StartMoment.HasValue && queue.StopMoment.HasValue;
    }
}
