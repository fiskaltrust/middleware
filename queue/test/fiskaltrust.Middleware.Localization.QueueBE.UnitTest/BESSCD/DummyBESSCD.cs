using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.be;

namespace fiskaltrust.Middleware.Localization.QueueBE.UnitTest.BESSCD
{
    internal class DummyBESSCD : IBESSCD
    {
        public DummyBESSCD() 
        {
        }

        public Task<EchoResponse> EchoAsync(EchoRequest echoRequest) => throw new NotImplementedException();
        public Task<BESSCDInfo> GetInfoAsync() => throw new NotImplementedException();
        public Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request) => throw new NotImplementedException();
    }
}