using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.Swissbit;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.AcceptanceTest.SCU
{
    public class SwissbitDESSCDTests : IDESSCDTests
    {
        private IDESSCD _instance = null;

        private const string _DEVICE_PATH = "h:";

        private ulong _lastCreatedSignatureState = 0;
        private string _publicKeyBase64 = "";
        private string _serialNumberOctet = "";

        protected override string TseClientId => "POS001";

        protected override ulong SignaturCounterOffset => _lastCreatedSignatureState;

        protected override TseInfo ExpectedInitializedTseInfo => new TseInfo
        {
            CertificationIdentification = "BSI-K-TR-0362",
            PublicKeyBase64 = _publicKeyBase64,
            SerialNumberOctet = _serialNumberOctet,
            SignatureAlgorithm = "ecdsa-plain-SHA384",
            MaxNumberOfStartedTransactions = 512,
            MaxNumberOfSignatures = 20000000L,
            MaxNumberOfClients = 100,
            MaxLogMemorySize = 6979321856,
            LogTimeFormat = "unixTime",
            FirmwareIdentification = "02000100",
            CurrentLogMemorySize = 18432L,
            CurrentNumberOfStartedTransactions = 0,
            CurrentNumberOfSignatures = (long) _lastCreatedSignatureState + 13,
            CertificatesBase64 = new List<string>
            {
                "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"
            },
            CurrentClientIds = new List<string>
            {
                TseClientId,
                "fiskaltrust.Middleware"
            },
            CurrentNumberOfClients = 2,
            CurrentStartedTransactionNumbers = new List<ulong>(),
            CurrentState = TseStates.Initialized
        };

        protected override TseInfo ExpectedUninitializedTseInfo => new TseInfo
        {
            CertificationIdentification = "BSI-K-TR-0362",
            PublicKeyBase64 = _publicKeyBase64,
            SerialNumberOctet = _serialNumberOctet,
            SignatureAlgorithm = "ecdsa-plain-SHA384",
            MaxNumberOfStartedTransactions = 512,
            MaxNumberOfSignatures = 20000000L,
            MaxNumberOfClients = 100,
            MaxLogMemorySize = 6979321856,
            LogTimeFormat = "unixTime",
            FirmwareIdentification = "02000100",
            CurrentLogMemorySize = 0,
            CurrentNumberOfStartedTransactions = 0,
            CurrentNumberOfSignatures = 0,
            CertificatesBase64 = new List<string>(),
            CurrentClientIds = new List<string>(),
            CurrentNumberOfClients = 0,
            CurrentStartedTransactionNumbers = new List<ulong>(),
            CurrentState = TseStates.Uninitialized
        };

        protected override TseInfo ExpectedTermiantedTseInfo => new TseInfo
        {
            CertificationIdentification = "BSI-K-TR-0362",
            PublicKeyBase64 = _publicKeyBase64,
            SerialNumberOctet = _serialNumberOctet,
            SignatureAlgorithm = "ecdsa-plain-SHA384",
            MaxNumberOfStartedTransactions = 512,
            MaxNumberOfSignatures = 20000000L,
            MaxNumberOfClients = 100,
            MaxLogMemorySize = 6979321856,
            LogTimeFormat = "unixTime",
            FirmwareIdentification = "02000100",
            CurrentLogMemorySize = 0,
            CurrentNumberOfStartedTransactions = 0,
            CurrentNumberOfSignatures = 0,
            CertificatesBase64 = new List<string>
            {
                "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"
            },
            CurrentClientIds = new List<string>(),
            CurrentNumberOfClients = 0,
            CurrentStartedTransactionNumbers = new List<ulong>(),
            CurrentState = TseStates.Terminated
        };

        protected override IDESSCD GetSystemUnderTest(Dictionary<string, object> configuration = null)
        {
            if (_instance != null && _instance is IDisposable disposable)
            {
                disposable.Dispose();
            }
            if (configuration == null)
            {
                configuration = new Dictionary<string, object>() { { "devicePath", _DEVICE_PATH } };
            }
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            var scuBootStrapper = new ScuBootstrapper
            {
                Configuration = configuration
            };
            scuBootStrapper.ConfigureServices(serviceCollection);
            _instance = serviceCollection.BuildServiceProvider().GetService<IDESSCD>();
            return _instance;
        }

        protected override IDESSCD GetResetSystemUnderTest(Dictionary<string, object> configuration = null)
        {
            var result = Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine(Directory.GetCurrentDirectory(), "tools/Swissbit", "wormCli.exe"),
                Arguments = $"{_DEVICE_PATH} info",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            });
            var tseInfo = JsonConvert.DeserializeObject<SwissbitCliTseInfo>(result.StandardOutput.ReadToEnd());
            _lastCreatedSignatureState = tseInfo.createdSignatures;
            _publicKeyBase64 = Convert.ToBase64String(ConvertHexToBytesX(tseInfo.publicKey));
            _serialNumberOctet = tseInfo.tseSerialNumber.ToUpper();
            var infoLog = "";
            var errorLog = "";
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(Directory.GetCurrentDirectory(), "tools/Swissbit", "wormCli.exe"),
                    Arguments = $"{_DEVICE_PATH} tseFactoryReset",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };
            process.Start();
            process.OutputDataReceived += (_, e) => infoLog += e.Data;
            process.ErrorDataReceived += (_, e) => errorLog += e.Data;
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new Exception("Process did exit with a non zero errorcode. " + errorLog);
            }
            return GetSystemUnderTest(configuration);
        }

        private static readonly byte[,] ByteLookup = new byte[,]
        {
            // low nibble
            {0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f},
            // high nibble
            {0x00, 0x10, 0x20, 0x30, 0x40, 0x50, 0x60, 0x70, 0x80, 0x90, 0xa0, 0xb0, 0xc0, 0xd0, 0xe0, 0xf0}
        };

        private static int HexToInt(char c)
        {
            switch (c)
            {
                case '0':
                    return 0;
                case '1':
                    return 1;
                case '2':
                    return 2;
                case '3':
                    return 3;
                case '4':
                    return 4;
                case '5':
                    return 5;
                case '6':
                    return 6;
                case '7':
                    return 7;
                case '8':
                    return 8;
                case '9':
                    return 9;
                case 'a':
                case 'A':
                    return 10;
                case 'b':
                case 'B':
                    return 11;
                case 'c':
                case 'C':
                    return 12;
                case 'd':
                case 'D':
                    return 13;
                case 'e':
                case 'E':
                    return 14;
                case 'f':
                case 'F':
                    return 15;
                default:
                    throw new FormatException("Unrecognized hex char " + c);
            }
        }

        private static byte[] ConvertHexToBytesX(string input)
        {
            var result = new byte[(input.Length + 1) >> 1];
            var lastcell = result.Length - 1;
            var lastchar = input.Length - 1;
            // count up in characters, but inside the loop will
            // reference from the end of the input/output.
            for (var i = 0; i < input.Length; i++)
            {
                // i >> 1    -  (i / 2) gives the result byte offset from the end
                // i & 1     -  1 if it is high-nibble, 0 for low-nibble.
                result[lastcell - (i >> 1)] |= ByteLookup[i & 1, HexToInt(input[lastchar - i])];
            }
            return result;
        }

        public void Dispose()
        {
            if (_instance is IDisposable disposable)
            {
                disposable.Dispose();
                Task.Delay(TimeSpan.FromSeconds(2)).Wait();
            }
        }
    }


    public class SwissbitCliTseInfo
    {
        public int capacity { get; set; }
        public int certificateExpirationDate { get; set; }
        public ulong createdSignatures { get; set; }
        public string customizationIdentifier { get; set; }
        public string formFactor { get; set; }
        public string hardwareVersion { get; set; }
        public bool hasChangedAdminPin { get; set; }
        public bool hasChangedPuk { get; set; }
        public bool hasChangedTimeAdminPin { get; set; }
        public bool hasPassedSelfTest { get; set; }
        public bool hasValidTime { get; set; }
        public int initializationState { get; set; }
        public bool isCtssInterfaceActive { get; set; }
        public bool isDataImportInProgress { get; set; }
        public bool isDevelopmentFirmware { get; set; }
        public bool isExportEnabledIfCspTestFails { get; set; }
        public int maxRegisteredClients { get; set; }
        public int maxSignatures { get; set; }
        public int maxStartedTransactions { get; set; }
        public int maxTimeSynchronizationDelay { get; set; }
        public int maxUpdateDelay { get; set; }
        public string publicKey { get; set; }
        public int registeredClients { get; set; }
        public int remainingSignatures { get; set; }
        public int size { get; set; }
        public string softwareVersion { get; set; }
        public int startedTransactions { get; set; }
        public int tarExportSizeInSectors { get; set; }
        public int timeUntilNextSelfTest { get; set; }
        public string tseDescription { get; set; }
        public string tseSerialNumber { get; set; }
    }
}
