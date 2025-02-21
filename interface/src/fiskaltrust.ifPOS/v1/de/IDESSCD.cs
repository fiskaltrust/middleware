using System.ServiceModel;
using System.Threading.Tasks;
#if WCF
using System.ServiceModel.Web;
#endif

namespace fiskaltrust.ifPOS.v1.de
{
    [ServiceContract]
    public interface IDESSCD
    {
        [OperationContract(Name = "v1/StartTransaction")]
#if WCF
        [WebInvoke(BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "v1/starttransaction", Method = "POST")]
#endif
        Task<StartTransactionResponse> StartTransactionAsync(StartTransactionRequest request);

        [OperationContract(Name = "v1/UpdateTransaction")]
#if WCF
        [WebInvoke(BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "v1/updatetransaction", Method = "POST")]
#endif
        Task<UpdateTransactionResponse> UpdateTransactionAsync(UpdateTransactionRequest request);

        [OperationContract(Name = "v1/FinishTransaction")]
#if WCF
        [WebInvoke(BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "v1/finishtransaction", Method = "POST")]
#endif
        Task<FinishTransactionResponse> FinishTransactionAsync(FinishTransactionRequest request);

        [OperationContract(Name = "v1/GetTseInfo")]
#if WCF
        [WebInvoke(BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "v1/tseinfo", Method = "GET")]
#endif
        Task<TseInfo> GetTseInfoAsync();

        [OperationContract(Name = "v1/SetTseState")]
#if WCF
        [WebInvoke(BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "v1/tsestate", Method = "POST")]
#endif
        Task<TseState> SetTseStateAsync(TseState state);

        [OperationContract(Name = "v1/RegisterClientId")]
#if WCF
        [WebInvoke(BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "v1/registerclientid", Method = "POST")]
#endif
        Task<RegisterClientIdResponse> RegisterClientIdAsync(RegisterClientIdRequest request);

        [OperationContract(Name = "v1/UnregisterClientId")]
#if WCF
        [WebInvoke(BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "v1/unregisterclientid", Method = "POST")]
#endif
        Task<UnregisterClientIdResponse> UnregisterClientIdAsync(UnregisterClientIdRequest request);

        [OperationContract(Name = "v1/ExecuteSetTseTime")]
#if WCF
        [WebInvoke(BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "v1/executesettsetime", Method = "POST")]
#endif
        Task ExecuteSetTseTimeAsync();

        [OperationContract(Name = "v1/ExecuteSelfTest")]
#if WCF
        [WebInvoke(BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "v1/executeselftest", Method = "POST")]
#endif
        Task ExecuteSelfTestAsync();

        [OperationContract(Name = "v1/StartExportSession")]
#if WCF
        [WebInvoke(BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "v1/startexportsession", Method = "POST")]
#endif
        Task<StartExportSessionResponse> StartExportSessionAsync(StartExportSessionRequest request);

        [OperationContract(Name = "v1/StartExportSessionByTimeStamp")]
#if WCF
        [WebInvoke(BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "v1/startexportsessionbytimestamp", Method = "POST")]
#endif
        Task<StartExportSessionResponse> StartExportSessionByTimeStampAsync(StartExportSessionByTimeStampRequest request);

        [OperationContract(Name = "v1/StartExportSessionByTransaction")]
#if WCF
        [WebInvoke(BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "v1/startexportsessionbytransaction", Method = "POST")]
#endif
        Task<StartExportSessionResponse> StartExportSessionByTransactionAsync(StartExportSessionByTransactionRequest request);

        [OperationContract(Name = "v1/ExportData")]
#if WCF
        [WebInvoke(BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "v1/exportdata", Method = "POST")]
#endif
        Task<ExportDataResponse> ExportDataAsync(ExportDataRequest request);

        [OperationContract(Name = "v1/EndExportSession")]
#if WCF
        [WebInvoke(BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "v1/endexportsession", Method = "POST")]
#endif
        Task<EndExportSessionResponse> EndExportSessionAsync(EndExportSessionRequest request);

        [OperationContract(Name = "v1/Echo")]
#if WCF
        [WebInvoke(BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "v1/echo", Method = "POST")]
#endif
        Task<ScuDeEchoResponse> EchoAsync(ScuDeEchoRequest request);
    }
}