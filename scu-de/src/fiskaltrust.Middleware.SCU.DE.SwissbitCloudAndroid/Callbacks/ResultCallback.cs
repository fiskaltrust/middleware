using DE.Fiskal.Connector.Android.Api;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudAndroid.Exceptions;
using System;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudAndroid.Callbacks
{
    internal class ResultCallback<T> : Java.Lang.Object, IResultCallback where T : class
    {
        private readonly TaskCompletionSource<T> _tcs;

        public ResultCallback(TaskCompletionSource<T> tcs)
        {
            _tcs = tcs;
        }

        public void Call(Java.Lang.Object result)
        {
            if (result is Failure failure)
            {
                _tcs.SetException(new WrappedFailureException(failure));
            }
            else if (result is Success success)
            {
                _tcs.SetResult(success.Value as T);
            }
            else
            {
                throw new NotSupportedException($"The result {result} from the FCC library could not be parsed.");
            }
        }
    }
}
