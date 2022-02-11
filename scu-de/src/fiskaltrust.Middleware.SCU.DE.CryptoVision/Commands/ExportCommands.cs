using fiskaltrust.Middleware.SCU.DE.CryptoVision.Helpers;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File.Models.Parameters;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Models.Commands
{
    public static class ExportCommands
    {
        public class ExportDataTseCommand : TseCommand
        {
            public ExportDataTseCommand(string clientId = null, uint maxNumberOfRecords = 0)
                : base(TseCommandCodeEnum.ExportData, 0x0000,
                     new TseStringParameter(clientId),
                     // transaction-number
                     new TseByteArrayParameter(new byte[] { 0xff, 0xff, 0xff, 0xff }),
                    // from-transaction-number, to-transaction-number
                    new TseByteArrayParameter(new byte[] { 0x00, 0x00, 0x00, 0x00 }), new TseByteArrayParameter(new byte[] { 0xff, 0xff, 0xff, 0xff }),
                    // from-timestamp, to-timestamp
                    new TseByteArrayParameter(new byte[] { 0x00, 0x00, 0x00, 0x00 }), new TseByteArrayParameter(new byte[] { 0xff, 0xff, 0xff, 0xff }),
                    // maximum number of records
                    new TseByteArrayParameter(maxNumberOfRecords.ToByteArray()))
            {
                // TODO parameter checks
            }

            public ExportDataTseCommand(uint transactionNumber, string clientId = null, uint maxNumberOfRecords = 0)
                : base(TseCommandCodeEnum.ExportData, 0x0000,
                     new TseStringParameter(clientId),
                     // transaction-number
                     new TseByteArrayParameter(transactionNumber.ToByteArray()),
                    // from-transaction-number, to-transaction-number
                    new TseByteArrayParameter(new byte[] { 0x00, 0x00, 0x00, 0x00 }), new TseByteArrayParameter(new byte[] { 0xff, 0xff, 0xff, 0xff }),
                    // from-timestamp, to-timestamp
                    new TseByteArrayParameter(new byte[] { 0x00, 0x00, 0x00, 0x00 }), new TseByteArrayParameter(new byte[] { 0xff, 0xff, 0xff, 0xff }),
                    // maximum number of records
                    new TseByteArrayParameter(maxNumberOfRecords.ToByteArray()))
            {
                // TODO parameter checks
            }

            public ExportDataTseCommand(uint fromTransactionNumber, uint toTransactionNumber, string clientId = null, uint maxNumberOfRecords = 0)
                : base(TseCommandCodeEnum.ExportData, 0x0000,
                     new TseStringParameter(clientId),
                     // transaction-number
                     new TseByteArrayParameter(new byte[] { 0xff, 0xff, 0xff, 0xff }),
                    // from-transaction-number, to-transaction-number
                    new TseByteArrayParameter(fromTransactionNumber.ToByteArray()), new TseByteArrayParameter(toTransactionNumber.ToByteArray()),
                    // from-timestamp, to-timestamp
                    new TseByteArrayParameter(new byte[] { 0x00, 0x00, 0x00, 0x00 }), new TseByteArrayParameter(new byte[] { 0xff, 0xff, 0xff, 0xff }),
                    // maximum number of records
                    new TseByteArrayParameter(maxNumberOfRecords.ToByteArray()))
            {
                // TODO parameter checks
            }

            public ExportDataTseCommand(long fromTimestamp, long toTimestamp, string clientId = null, uint maxNumberOfRecords = 0)
                : base(TseCommandCodeEnum.ExportData, 0x0000,
                     new TseStringParameter(clientId),
                     // transaction-number
                     new TseByteArrayParameter(new byte[] { 0xff, 0xff, 0xff, 0xff }),
                    // from-transaction-number, to-transaction-number
                    new TseByteArrayParameter(new byte[] { 0x00, 0x00, 0x00, 0x00 }), new TseByteArrayParameter(new byte[] { 0xff, 0xff, 0xff, 0xff }),
                    // from-timestamp, to-timestamp
                    new TseByteArrayParameter(fromTimestamp.ToByteArray()), new TseByteArrayParameter(toTimestamp.ToByteArray()),
                    // maximum number of records
                    new TseByteArrayParameter(maxNumberOfRecords.ToByteArray()))
            {
                // TODO parameter checks
            }
        }
    }
}
