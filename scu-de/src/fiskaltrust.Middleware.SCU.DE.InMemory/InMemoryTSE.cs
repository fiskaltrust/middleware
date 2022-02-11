using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Numerics;
using fiskaltrust.Middleware.SCU.DE.InMemory.Models;
using fiskaltrust.ifPOS.v1.de;
using System.Formats.Asn1;
using System.Globalization;

namespace fiskaltrust.Middleware.SCU.DE.InMemory
{
    public class InMemoryTSE : IDisposable
    {

        public static string TssCertificateSerial = "88881111114c28844b85485e35b2b6c05cca2b6719279dd77f84de9999999999";
        public static string TssPublicKey = "Test111111ZIzj0CAQYIKoZIzj0DAQcDQgAEmSQnu2fXcDhKZ8HprEKxMKdE/NAtnYCkYtZRzrMj9iXai84yAXY2fFHm5DUkJt0UL5b1Thrtoq6+IJ9999999999";
        public static string TssResultVersion = "1.1.1";
        public static string CertificationIdentification = "Test-TR-001 [TSE in Memory]";
        public static string Certificate = "-----BEGIN CERTIFICATE-----\nTestCertificate\n-----END CERTIFICATE-----";
        public static string SignatureAlgorithm = @"0.4.0.127.0.7.1.1.4.1.3";
        public static string LogTimeFormat = "utcTimeWithSeconds";
        private const string TransitionLog = @"0.4.0.127.0.7.3.7.1.1";
        private readonly ConcurrentDictionary<string, ClientDto> _registeredClients = new ConcurrentDictionary<string, ClientDto>();
        private readonly ConcurrentDictionary<int, TransactionDto> _inMemoryTransactions = new ConcurrentDictionary<int, TransactionDto>();
        private readonly Guid _tssId = Guid.NewGuid();
        private int _transactioNo = 0, _signatureCounter = 0;
        private TseState _tseState;

        public InMemoryTSE()
        {
            _tseState = new TseState
            {
                CurrentState = TseStates.Initialized
            };
        }

        public TseInfo GetTseInfo()
        {
            return new TseInfo
            {
                CurrentNumberOfClients = _registeredClients.Count,
                CurrentNumberOfStartedTransactions = _inMemoryTransactions.Count,
                SerialNumberOctet = TssCertificateSerial,
                PublicKeyBase64 = TssPublicKey,
                FirmwareIdentification = TssResultVersion,
                CertificationIdentification = CertificationIdentification,
                MaxNumberOfClients = int.MaxValue,
                MaxNumberOfStartedTransactions = int.MaxValue,
                CertificatesBase64 = new List<string>
                    {
                        Convert.ToBase64String(Encoding.UTF8.GetBytes(Certificate))
                    },
                CurrentClientIds = _registeredClients.Values.Select(x => x.SerialNumber),
                SignatureAlgorithm = SignatureAlgorithm,
                CurrentLogMemorySize = -1,
                CurrentNumberOfSignatures = _inMemoryTransactions.Count,
                LogTimeFormat = LogTimeFormat,
                MaxLogMemorySize = long.MaxValue,
                MaxNumberOfSignatures = long.MaxValue,
                CurrentStartedTransactionNumbers = _inMemoryTransactions.Values.Select(x => (ulong) x.Number).ToList(),
                CurrentState = _tseState.CurrentState
            };
        }

        internal void GetOrRegisterClient(string serialNumber)
        {
            _ = GetOrCreateClient(serialNumber);
        }

        private ClientDto GetOrCreateClient(string serialNumber)
        {
            if (!_registeredClients.ContainsKey(serialNumber))
            {
                var newClient = new ClientDto()
                {
                    Id = Guid.NewGuid(),
                    SerialNumber = serialNumber,
                    TimeCreation = (int) DateTime.Now.TimeOfDay.TotalSeconds,
                    TssId = _tssId
                };

                _ = _registeredClients.TryAdd(serialNumber, newClient);
                return newClient;
            }
            return _registeredClients[serialNumber];
        }

        internal TransactionDto StartTransactionAsync(TransactionRequestDto transactionRequest)
        {
            var transactionDto = CreateTransactionDto(transactionRequest.Data.RawData.ProcessData, transactionRequest.Data.RawData.ProcessType, transactionRequest.ClientId);
            transactionDto.Number = (uint) Interlocked.Increment(ref _transactioNo);
            transactionDto.LatestRevision = 1;

            _ = _inMemoryTransactions.TryAdd((int) transactionDto.Number, transactionDto);
            return transactionDto;
        }

        private TransactionDto CreateTransactionDto(string processData64, string processType, string clientId)
        {
            var id = Guid.NewGuid();
            var processData = string.Empty;
            if (!string.IsNullOrEmpty(processData64))
            {
                var base64EncodedBytes = Convert.FromBase64String(processData64);
                processData = Encoding.UTF8.GetString(base64EncodedBytes);
            }
          
            var transactionDto =  new TransactionDto()
            {
                TimeStart = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc),
                Schema = new TransactionDataDto()
                {
                    RawData = new RawData()
                    {
                        ProcessData = processData,
                        ProcessType = processType
                    }
                },
                CertificateSerial = id.ToString(),
                ClientSerialNumber = clientId,

                Log = new TransactionLogDto()
                {
                    Timestamp = (long) Utilities.ToUnixTime(DateTime.Now)
                }

            };
            transactionDto.Signature = new TransactionSignatureDto()
            {
                Algorithm = SignatureAlgorithm,
                SignatureCounter = (uint) Interlocked.Increment(ref _signatureCounter),
                Value = Encoding.UTF8.GetString(CreateSignatureData(transactionDto))
            };
             return transactionDto;
        }

        public static ExportDataResponse CreateExportDataResponse(ExportDataRequest request)
        {
            _ = request ?? throw new ArgumentNullException(nameof(request));
            return new ExportDataResponse
            {
                TokenId = request.TokenId,
                TarFileByteChunkBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("This is a TestTarFile!")),
                TotalTarFileSizeAvailable = true,
                TotalTarFileSize = 0,
                TarFileEndOfFile = true
            };
        }

        internal TseState SetTseState(TseState tseState)
        {
            _tseState = tseState;
            return _tseState;
        }

        internal TransactionDto PutTransactionRequestWithStateAsync(ulong transactionNumber, long lastRevisionForTransaction, TransactionRequestDto transactionRequest)
        {
            var transactionDto = CreateTransactionDto(transactionRequest.Data.RawData.ProcessData, transactionRequest.Data.RawData.ProcessType, transactionRequest.ClientId);
            transactionDto.Number = (uint) Convert.ToInt32(transactionNumber);
            transactionDto.LatestRevision = Convert.ToInt32(lastRevisionForTransaction);
            _ = _inMemoryTransactions.AddOrUpdate((int) transactionDto.Number, transactionDto, (key, oldValue) => transactionDto);
            return transactionDto;
        }

        internal TransactionDto GetTransactionDtoAsync(ulong transactionNumber)
        {
            _ = _inMemoryTransactions.TryGetValue(Convert.ToInt32(transactionNumber), out var transactionDto);
            return transactionDto;
        }

        internal void UnregisterClient(string serialNumer)
        {
            _ = _registeredClients.TryRemove(serialNumer, out _);
        }

        internal List<string> GetClientIds() => _registeredClients.Keys.ToList();

        private byte[] CreateSignatureData(TransactionDto transaction)
        {
            var signatureData = new AsnWriter(AsnEncodingRules.DER);
            signatureData.WriteInteger(2); // version
            signatureData.WriteObjectIdentifier(TransitionLog); // certifiedDataType
            // certifiedData
            signatureData.WriteCharacterString(UniversalTagNumber.PrintableString, "FinishTransaction", new Asn1Tag(TagClass.ContextSpecific, 0));
            signatureData.WriteCharacterString(UniversalTagNumber.PrintableString, transaction.ClientSerialNumber, new Asn1Tag(TagClass.ContextSpecific, 1));
            var processDataBytes = Encoding.UTF8.GetBytes(transaction.Schema.RawData.ProcessData);
            signatureData.WriteOctetString(processDataBytes, new Asn1Tag(TagClass.ContextSpecific, 2));
            var processType = transaction.Schema.RawData.ProcessType ?? string.Empty;
            signatureData.WriteCharacterString(UniversalTagNumber.PrintableString, processType, new Asn1Tag(TagClass.ContextSpecific, 3));
            signatureData.WriteInteger(transaction.Number, new Asn1Tag(TagClass.ContextSpecific, 5));
            var byteSerial = BigInteger.Parse(TssCertificateSerial, NumberStyles.HexNumber)
                .ToByteArray()
                .Reverse()
                .ToArray();
            signatureData.WriteOctetString(byteSerial);
            _ = signatureData.PushSequence(); 
            signatureData.WriteObjectIdentifier(SignatureAlgorithm);
            signatureData.PopSequence();
            signatureData.WriteInteger(_signatureCounter);
            signatureData.WriteUtcTime(transaction.TimeStart);
            return signatureData.Encode();
        }

        public void Dispose() {

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _registeredClients.Clear();
                _inMemoryTransactions.Clear();
            }
        }

    }
}
