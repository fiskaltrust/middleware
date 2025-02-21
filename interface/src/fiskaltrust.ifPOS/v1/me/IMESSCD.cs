using System.ServiceModel;
using System.Threading.Tasks;

namespace fiskaltrust.ifPOS.v1.me
{
    /// <summary>
    /// The interface to communicate with fiskaltrust's SCUs for Montenegro.
    /// </summary>
    [ServiceContract]
    public interface IMESSCD
    {
        /// <summary>
        /// Registers an electronic cash device (TCR) at the central invoice register (CIS), and returns the TCR code that is used for further operations.
        /// </summary>
        [OperationContract(Name = "v2/RegisterTCR")]
        Task<RegisterTcrResponse> RegisterTcrAsync(RegisterTcrRequest registerTCRRequest);

        /// <summary>
        /// Unregisters an electronic cash device (TCR) from the central invoice register (CIS).
        /// </summary>
        [OperationContract(Name = "v2/UnregisterTCR")]
        Task UnregisterTcrAsync(UnregisterTcrRequest registerTCRRequest);

        /// <summary>
        /// Registers a cash deposit to the cash registers at the central invoice register (CIS).
        /// </summary>
        [OperationContract(Name = "v2/RegisterCashDeposit")]
        Task<RegisterCashDepositResponse> RegisterCashDepositAsync(RegisterCashDepositRequest registerCashDepositRequest);

        /// <summary>
        /// Registers a cash withdrawal from the cash registers at the central invoice register (CIS).
        /// </summary>
        [OperationContract(Name = "v2/RegisterCashWithdrawal")]
        Task RegisterCashWithdrawalAsync(RegisterCashWithdrawalRequest registerCashWithdrawalRequest);

        /// <summary>
        /// Computes the IIC (IKOV) of an invoice.
        /// </summary>
        [OperationContract(Name = "v2/ComputeIIC")]
        Task<ComputeIICResponse> ComputeIICAsync(ComputeIICRequest computeIICRequest);

        /// <summary>
        /// Registers an invoice (i.e. a receipt in fiskaltrust's terminology) at the central invoice register (CIS).
        /// </summary>        
        [OperationContract(Name = "v2/RegisterInvoice")]
        Task<RegisterInvoiceResponse> RegisterInvoiceAsync(RegisterInvoiceRequest registerInvoiceRequest);

        /// <summary>
        /// Returns the input message (can be used for a communication test with the SCU).
        /// </summary>
        [OperationContract(Name = "v2/Echo")]
        Task<ScuMeEchoResponse> EchoAsync(ScuMeEchoRequest request);
    }
}