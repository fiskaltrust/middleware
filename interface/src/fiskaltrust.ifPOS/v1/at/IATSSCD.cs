using System.ServiceModel;
using System.Threading.Tasks;
#if WCF
using System.ServiceModel.Web;
#endif

namespace fiskaltrust.ifPOS.v1.at
{
    /// <summary>
    /// This interface is applicable only for the Austrian market and enables direct communication with the signature creation device for own purposes: it can be used for testing if the service is running (“Echo” call), for getting the certificate (“Certificate” call), or signing autono-mously (“Sign” call).
    /// </summary>
    [ServiceContract]
    public interface IATSSCD : v0.IATSSCD
    {
        /// <summary>
        /// Get the certificate of the signature creation device
        /// </summary>
        [OperationContract(Name = "v1/Certificate")]
#if WCF
        [WebInvoke(BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "v1/certificate", Method = "GET")]
#endif
        Task<CertificateResponse> CertificateAsync();

        /// <summary>
        /// Get the short name of the certificate service provider for RKSV
        /// </summary>
        [OperationContract(Name = "v1/ZDA")]
#if WCF
        [WebInvoke(BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "v1/zda", Method = "GET")]
#endif
        Task<ZdaResponse> ZdaAsync();

        /// <summary>
        /// Sign data with the signature creation device
        /// </summary>
        [OperationContract(Name = "v1/Sign")]
#if WCF
        [WebInvoke(BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "v1/sign", Method = "POST")]
#endif
        Task<SignResponse> SignAsync(SignRequest signRequest);

        /// <summary>
        /// Function to test communication
        /// </summary>
        [OperationContract(Name = "v1/Echo")]
#if WCF
        [WebInvoke(BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "v1/echo", Method = "POST")]
#endif
        Task<EchoResponse> EchoAsync(EchoRequest echoRequest);
    }
}