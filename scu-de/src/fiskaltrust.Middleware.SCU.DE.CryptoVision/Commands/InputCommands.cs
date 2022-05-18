using fiskaltrust.Middleware.SCU.DE.CryptoVision.Helpers;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File.Models.Parameters;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Models.Commands
{
    public static class InputCommands
    {
        public static TseCommand CreateStartTransactionTseCommand(string clientId, byte[] processData, string processType) => new TseCommand(TseCommandCodeEnum.StartTransaction, 0x0000, new TseStringParameter(clientId), new TseByteArrayParameter(processData), new TseStringParameter(processType), new TseByteArrayParameter(null));

        public static TseCommand CreateUpdateTransactionTseCommand(string clientId, uint transactionNumber, byte[] processData, string processType) => new TseCommand(TseCommandCodeEnum.UpdateTransaction, 0x0000, new TseByteArrayParameter(transactionNumber.ToByteArray()), new TseStringParameter(clientId), new TseByteArrayParameter(processData), new TseStringParameter(processType));
        
        public static TseCommand CreateFinishTransactionTseCommand(string clientId, uint transactionNumber, byte[] processData, string processType) => new TseCommand(TseCommandCodeEnum.FinishTransaction, 0x0000, new TseByteArrayParameter(transactionNumber.ToByteArray()), new TseStringParameter(clientId), new TseByteArrayParameter(processData), new TseStringParameter(processType), new TseByteArrayParameter(null));
    }
}
