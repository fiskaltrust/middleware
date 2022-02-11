using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models.Enums;

#pragma warning disable
namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Commands
{
    public class TransactionTseCommandsProvider
    {
        private readonly TseCommunicationCommandHelper _tseCommunicationHelper;

        public TransactionTseCommandsProvider(TseCommunicationCommandHelper tseCommunicationHelper)
        {
            _tseCommunicationHelper = tseCommunicationHelper;
        }

        public StartTransactionEndResult StartTransaction(string clientId, byte[] processData, string processType)
        {
            try
            {
                _tseCommunicationHelper.SetClientId(clientId);
                StartTransactionInit(processData.Length, processType, 0);
                TransactionData(processData);
                return StartTransactionEnd();
            }
            catch (Exception ex)
            {
                var excMfcStatus = _tseCommunicationHelper.GetMfcStatus();
                if (excMfcStatus.IsTransactionOpen)
                {
                    TransactionVoid();
                }
                throw;
            }
        }

        private void StartTransactionInit(long processDataSize, string processType, long additionalSize)
        {
            var parameters = new List<string> {
                RequestHelper.AsAsciiDigit(processDataSize),
                RequestHelper.AsAsn1Printable(processType),
                RequestHelper.AsAsciiDigit(additionalSize)
            };
            _tseCommunicationHelper.ExecuteCommandWithoutResponse(DieboldNixdorfCommand.StartTransactionInit, parameters);
        }

        private StartTransactionEndResult StartTransactionEnd()
        {
            var resultParameters = _tseCommunicationHelper.ExecuteCommandWithResponse(DieboldNixdorfCommand.StartTransactionEnd);
            return new StartTransactionEndResult
            {
                LogTime = ResponseHelper.FromDateTime(resultParameters[0]),
                TransactionNo = (ulong) ResponseHelper.GetResultForAsciiDigit(resultParameters[1]),
                SerialNoBase64 = ResponseHelper.GetResultForBase64(resultParameters[2]),
                SignatureCounter = (ulong) ResponseHelper.GetResultForAsciiDigit(resultParameters[3]),
                SignatureBase64 = ResponseHelper.GetResultForBase64(resultParameters[4])
            };
        }

        public UpdateTransactionEndResult UpdateTransaction(string clientId, ulong transactionNumber, byte[] processData, string processType)
        {
            try
            {
                _tseCommunicationHelper.SetClientId(clientId);
                UpdateTransactionInit(transactionNumber, processData.Length, processType, 0);
                TransactionData(processData);
                return UpdateTransactionEnd();
            }
            catch (Exception ex)
            {
                var excMfcStatus = _tseCommunicationHelper.GetMfcStatus();
                if (excMfcStatus.IsTransactionOpen)
                {
                    TransactionVoid();
                }
                throw;
            }
        }

        private void UpdateTransactionInit(ulong transactionNumber, long processDataSize, string processType, long additionalSize)
        {
            var parameters = new List<string> {
                RequestHelper.AsAsciiDigit((long)transactionNumber),
                RequestHelper.AsAsciiDigit(processDataSize),
                RequestHelper.AsAsn1Printable(processType),
                RequestHelper.AsAsciiDigit(additionalSize)
            };
            _tseCommunicationHelper.ExecuteCommandWithoutResponse(DieboldNixdorfCommand.UpdateTransactionInit, parameters);
        }

        private UpdateTransactionEndResult UpdateTransactionEnd()
        {
            var resultParameters = _tseCommunicationHelper.ExecuteCommandWithResponse(DieboldNixdorfCommand.UpdateTransactionEnd);
            return new UpdateTransactionEndResult
            {
                LogTime = ResponseHelper.FromDateTime(resultParameters[0]),
                SignatureCounter = (ulong) ResponseHelper.GetResultForAsciiDigit(resultParameters[1]),
                SignatureBase64 = ResponseHelper.GetResultForBase64(resultParameters[2])
            };
        }

        public FinishTransactionEndResult FinishTransaction(string clientId, ulong transactionNumber, byte[] processData, string processType)
        {
            try
            {
                _tseCommunicationHelper.SetClientId(clientId);
                FinishTransactionInit(transactionNumber, processData.Length, processType, 0);
                TransactionData(processData);
                return FinishTransactionEnd();
            }
            catch (Exception ex)
            {
                var excMfcStatus = _tseCommunicationHelper.GetMfcStatus();
                if (excMfcStatus.IsTransactionOpen)
                {
                    TransactionVoid();
                }
                throw;
            }
        }

        private void FinishTransactionInit(ulong transactionNumber, long processDataSize, string processType, long additionalSize)
        {
            var parameters = new List<string> {
                RequestHelper.AsAsciiDigit((long) transactionNumber),
                RequestHelper.AsAsciiDigit(processDataSize),
                RequestHelper.AsAsn1Printable(processType),
                RequestHelper.AsAsciiDigit(additionalSize)
            };
            _tseCommunicationHelper.ExecuteCommandWithoutResponse(DieboldNixdorfCommand.FinishTransactionInit, parameters);
        }

        private FinishTransactionEndResult FinishTransactionEnd()
        {
            var resultParameters = _tseCommunicationHelper.ExecuteCommandWithResponse(DieboldNixdorfCommand.FinishTransactionEnd);
            return new FinishTransactionEndResult
            {
                LogTime = ResponseHelper.FromDateTime(resultParameters[0]),
                SignatureCounter = (ulong) ResponseHelper.GetResultForAsciiDigit(resultParameters[1]),
                SignatureBase64 = ResponseHelper.GetResultForBase64(resultParameters[2])
            };
        }

        private void TransactionData(byte[] packetData)
        {
            if (packetData.Length == 0)
            {
                return;
            }
            if (packetData.Length > 512)
            {
                var packets = SplitByteArray(packetData, 512).ToList();
                for (var i = 0; i < packets.Count; i++)
                {
                    var parameters = new List<string> {
                        RequestHelper.AsAsciiDigit(i),
                        RequestHelper.AsBase64(packets[i])
                    };
                    _tseCommunicationHelper.ExecuteCommandWithoutResponse(DieboldNixdorfCommand.TransactionData, parameters);
                }
            }
            else
            {
                var parameters = new List<string> {
                    RequestHelper.AsAsciiDigit(0),
                    RequestHelper.AsBase64(packetData)
                };
                _tseCommunicationHelper.ExecuteCommandWithoutResponse(DieboldNixdorfCommand.TransactionData, parameters);
            }
        }

        private IEnumerable<byte[]> SplitByteArray(byte[] packetData, int desiredLength)
        {
            int packetDataLength = packetData.Length;
            byte[] result = null;

            int i = 0;
            for (; packetDataLength > (i + 1) * desiredLength; i++)
            {
                result = new byte[desiredLength];
                Array.Copy(packetData, i * desiredLength, result, 0, desiredLength);
                yield return result;
            }

            int bufferLeft = packetDataLength - i * desiredLength;
            if (bufferLeft > 0)
            {
                result = new byte[bufferLeft];
                Array.Copy(packetData, i * desiredLength, result, 0, bufferLeft);
                yield return result;
            }
        }

        public void TransactionVoid() => _tseCommunicationHelper.ExecuteCommandWithoutResponse(DieboldNixdorfCommand.TransactionVoid);
    }
}
