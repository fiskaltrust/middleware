using System;
using System.ServiceModel;
#if WCF
using System.ServiceModel.Web;
#endif

namespace fiskaltrust.ifPOS.v0
{
    /// <summary>
    /// This interface is applicable only for the Austrian market and enables direct communication with the signature creation device for own purposes: it can be used for testing if the service is running (“Echo” call), for getting the certificate (“Certificate” call), or signing autono-mously (“Sign” call).
    /// </summary>
    [ServiceContract]
    public interface IATSSCD
    {
        /// <summary>
        /// Get the certificate of the signaturcreationdevice
        /// </summary>
        /// <returns>certificate byte data</returns>
        [OperationContract]
#if WCF
        [WebInvoke(BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "v0/certificate", Method = "GET")]
#endif
        [Obsolete("This method is obsolete, use CertificateAsync instead.")]
        byte[] Certificate();

        /// <summary>
        /// Get the certificate of the signaturcreationdevice async begin pattern
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        [OperationContract(AsyncPattern = true)]
        [Obsolete("This method is obsolete, use CertificateAsync instead.")]
        IAsyncResult BeginCertificate(AsyncCallback callback, object state);

        /// <summary>
        /// Get the certificate of the signaturcreationdevice async end pattern
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        [Obsolete("This method is obsolete, use CertificateAsync instead.")]
        byte[] EndCertificate(IAsyncResult result);

        /// <summary>
        /// Get the certificate service operator short sign for rksv
        /// </summary>
        /// <returns>operator sign</returns>
        [OperationContract]
#if WCF
        [WebInvoke(BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "v0/zda", Method = "GET")]
#endif
        [Obsolete("This method is obsolete, use ZdaAsync instead.")]
        string ZDA();

        /// <summary>
        /// Get the certificate service operator short sign for rksv async begin pattern
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        [OperationContract(AsyncPattern = true)]
        [Obsolete("This method is obsolete, use ZdaAsync instead.")]
        IAsyncResult BeginZDA(AsyncCallback callback, object state);

        /// <summary>
        /// Get the certificate service operator short sign for rksv async end pattern
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        [Obsolete("This method is obsolete, use ZdaAsync instead.")]
        string EndZDA(IAsyncResult result);

        /// <summary>
        /// Sign data with the signaturcreationdevice
        /// </summary>
        /// <param name="data">payload data</param>
        /// <returns>signature data</returns>
        [OperationContract]
#if WCF
        [WebInvoke(BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "v0/sign", Method = "POST")]
#endif
        [Obsolete("This method is obsolete, use SignAsync instead.")]
        byte[] Sign(byte[] data);

        /// <summary>
        /// Sign data with the signaturcreationdevice async begin pattern
        /// </summary>
        /// <param name="data"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        [OperationContract(AsyncPattern = true)]
        [Obsolete("This method is obsolete, use SignAsync instead.")]
        IAsyncResult BeginSign(byte[] data, AsyncCallback callback, object state);

        /// <summary>
        /// Sign data with the signaturcreationdevice async end pattern
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        [Obsolete("This method is obsolete, use SignAsync instead.")]
        byte[] EndSign(IAsyncResult result);

        /// <summary>
        /// Function to test communication
        /// </summary>
        /// <param name="message">The test message</param>
        /// <returns>The test message</returns>
        [OperationContract]
#if WCF
        [WebInvoke(BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "v0/echo", Method = "POST")]
#endif
        [Obsolete("This method is obsolete, use EchoAsync instead.")]
        string Echo(string message);

        /// <summary> 
        /// Function to test communication async begin pattern
        /// </summary>
        /// <param name="message"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        [OperationContract(AsyncPattern = true)]
        [Obsolete("This method is obsolete, use EchoAsync instead.")]
        IAsyncResult BeginEcho(string message, AsyncCallback callback, object state);

        /// <summary>
        /// Function to test communication async end pattern
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        [Obsolete("This method is obsolete, use EchoAsync instead.")]
        string EndEcho(IAsyncResult result);
    }
}