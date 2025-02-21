using System;

namespace fiskaltrust.ifPOS.Tests.Helpers
{
    public class DummyATSSCD : ifPOS.v0.IATSSCD
    {
        public static string zda = "ZDA";
        public static byte[] certificate = Guid.NewGuid().ToByteArray();

        private delegate byte[] Sign_Delegate(byte[] data);
        private delegate string Echo_Delegate(string message);
        private delegate byte[] Certificate_Delegate();

        public IAsyncResult BeginCertificate(AsyncCallback callback, object state)
        {
            var d = new Certificate_Delegate(Certificate);
            var r = d.BeginInvoke(callback, d);
            return r;
        }

        public byte[] EndCertificate(IAsyncResult result)
        {
            var d = (Certificate_Delegate)result.AsyncState;
            return d.EndInvoke(result);
        }

        public byte[] Certificate() => certificate;

        public IAsyncResult BeginEcho(string message, AsyncCallback callback, object state)
        {
            var d = new Echo_Delegate(Echo);
            var r = d.BeginInvoke(message, callback, d);
            return r;
        }

        public string EndEcho(IAsyncResult result)
        {
            var d = (Echo_Delegate)result.AsyncState;
            return d.EndInvoke(result);
        }

        public string Echo(string message) => message;

        public IAsyncResult BeginSign(byte[] data, AsyncCallback callback, object state)
        {
            var d = new Sign_Delegate(Sign);
            var r = d.BeginInvoke(data, callback, d);
            return r;
        }

        public byte[] EndSign(IAsyncResult result)
        {
            var d = (Sign_Delegate)result.AsyncState;
            return d.EndInvoke(result);
        }

        public byte[] Sign(byte[] data) => data;

        private delegate string ZDA_Delegate();
        public IAsyncResult BeginZDA(AsyncCallback callback, object state)
        {
            var d = new ZDA_Delegate(ZDA);
            var r = d.BeginInvoke(callback, d);
            return r;
        }

        public string EndZDA(IAsyncResult result)
        {
            var d = (ZDA_Delegate)result.AsyncState;
            return d.EndInvoke(result);
        }
        public string ZDA() => zda;
    }
}