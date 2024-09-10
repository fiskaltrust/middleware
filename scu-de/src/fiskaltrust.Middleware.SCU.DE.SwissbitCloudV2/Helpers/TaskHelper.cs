using System;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Helpers
{
    public static class TaskHelper
    {
        public static async void ExecuteInBackgroundThread(this Task task, Action<Exception> onException = null)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                onException?.Invoke(ex);
            }
        }
    }
}
