using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.SCU.DE.Swissbit.Helpers
{
    public static class TaskHelper
    {
        [SuppressMessage("Microsoft.Design",
            "CA1030:UseEventsWhereAppropriate",
            Justification = "This warning comes up when you use the word `Fire` in a method name. This method specifically raises events, and so does not need changing.")]
        public static async void FireAndForget(this Task task, Action<Exception> onException = null)
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
