using Android.OS;
using DE.Fiskal.Connector.Android.Client.Library;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudAndroid.Exceptions;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudAndroid.Callbacks
{
    internal class FccClientStartCallback : Java.Lang.Object, IFccClientStartCallback
    {
        private readonly TaskCompletionSource<global::DE.Fiskal.Connector.Android.Api.IFccAndroid> _tcs;

        public FccClientStartCallback(TaskCompletionSource<global::DE.Fiskal.Connector.Android.Api.IFccAndroid> tcs)
        {
            _tcs = tcs;
        }

        public IBinder AsBinder() => null;

        public void OnError(FccClientError error)
        {
            _tcs.SetException(new WrappedClientException(error));
        }

        public void OnSuccess(global::DE.Fiskal.Connector.Android.Api.IFccAndroid fccAndroid)
        {
            _tcs.SetResult(fccAndroid);
        }
    }
}
