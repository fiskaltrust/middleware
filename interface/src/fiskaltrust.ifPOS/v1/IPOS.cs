using System.ServiceModel;
using System.Threading.Tasks;
#if STREAMING
using System.Collections.Generic;
#endif
#if WCF
using System.ServiceModel.Web;
#endif

namespace fiskaltrust.ifPOS.v1
{
    [ServiceContract]
    public interface IPOS : v0.IPOS
    {
        /// <summary>
        /// The fiskaltrust.Middleware provides the 3 basic functions "echo", "sign" and "journal".
        /// </summary>
        /// <param name="request"></param>
        /// <returns>The functions "echo" and "sign" return bare-objects, the function "journal" returns a wrapped-object.</returns>
        [OperationContract(Name = "v1/Sign")]
#if WCF
        [WebInvoke(BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "v1/sign")]
#endif
        Task<ReceiptResponse> SignAsync(ReceiptRequest request);

#if STREAMING
        [OperationContract(Name = "v1/Journal")]
        IAsyncEnumerable<JournalResponse> JournalAsync(JournalRequest request);
#endif

        [OperationContract(Name = "v1/Echo")]
#if WCF
        [WebInvoke(BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "v1/echo")]
#endif
        Task<EchoResponse> EchoAsync(EchoRequest message);
    }
}