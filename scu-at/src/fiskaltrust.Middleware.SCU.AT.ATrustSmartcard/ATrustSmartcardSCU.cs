using fiskaltrust.ifPOS.v1.at;
using fiskaltrust.Middleware.SCU.AT.ATrustSmartcard;
using fiskaltrust.Middleware.SCU.AT.ATrustSmartcard.Enums;
using fiskaltrust.Middleware.SCU.AT.ATrustSmartcard.Helpers;
using fiskaltrust.Middleware.SCU.AT.ATrustSmartcard.Services;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto;
using PCSC;
using PCSC.Iso7816;

namespace fiskaltrust.Middleware.SCU.AT.ATrustSmartcard
{
    public class ATrustSmartcardSCU : IATSSCD, IDisposable
    {
        private const string ZDA_NAME = "AT1";

        private delegate string Echo_Delegate(string message);
        private delegate byte[] Certificate_Delegate();
        private delegate string ZDA_Delegate();
        private delegate byte[] Sign_Delegate(byte[] data);

        private readonly System.Timers.Timer _watchdogTimer;
        private readonly ATrustSmartcardSCUConfiguration _configuration;
        private readonly LockHelper _lockHelper;
        private readonly CardServiceFactory _cardServiceFactory;
        private readonly ILogger<ATrustSmartcardSCU> _logger;

        private ReaderMode _currentReaderMode = 0;
        private int _currentReaderIndex = -1;
        private SCardContext _cardContext;
        private IsoReader _isoReader;
        private SCardReader _cardReader;
        private string[] _readers;
        private byte[] _certificate;
        private ISigner verifier;
        private CardService _card;
        
        public ATrustSmartcardSCU(ATrustSmartcardSCUConfiguration configuration, LockHelper lockHelper, CardServiceFactory cardServiceFactory, ILogger<ATrustSmartcardSCU> logger)
        {
            _configuration = configuration;
            _lockHelper = lockHelper;
            _cardServiceFactory = cardServiceFactory;
            _logger = logger;

            try
            {
                InitalizeReader();
            }
            catch (Exception x)
            {
                _logger.LogError(x, "An error occurred while initializing the reader.");
            }

            _watchdogTimer = new System.Timers.Timer(configuration.WatchdogTimeoutMs)
            {
                AutoReset = false
            };
            _watchdogTimer.Elapsed += WatchdogElapsed;
            _watchdogTimer.Start();
        }

        private void WatchdogElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                _lockHelper.ExecuteReaderCommandLocked(_currentReaderIndex, Operation.WaitUntilReaderIsAvailable, () => true);
                
                if (_configuration.HealthCheck && _currentReaderMode != ReaderMode.DisabledOrUnknown)
                {
                    _lockHelper.ExecuteReaderCommandLocked(_currentReaderIndex, Operation.CardHealthCheck, () =>
                    {
                        try
                        {
                            _card.SelectApplication();
                        }
                        catch (Exception x)
                        {
                            _logger.LogTrace(x, "An error occurred while calling SelectApplication in the watchdog timer.");
                            _currentReaderMode = ReaderMode.DisabledOrUnknown;
                        }
                    });
                }

                if (_currentReaderMode == ReaderMode.DisabledOrUnknown)
                {
                    InitalizeReader();
                }
            }
            catch (Exception x)
            {
                _logger.LogError(x, "An error occurred while executing the WatchDog timer.");
            }
            finally
            {
                _watchdogTimer.Start();
            }
        }
      
        private void InitalizeReader()
        {
            _lockHelper.ExecuteReaderCommandLocked(_currentReaderIndex, Operation.InitalizeReader, () =>
            {
                _currentReaderMode = ReaderMode.DisabledOrUnknown;

                if (_cardContext == null)
                {
                    _cardContext = new SCardContext();
                    _cardContext.Establish(SCardScope.System);
                    _logger.LogTrace("PCSC context successfully established.");
                }

                if (_cardContext.CheckValidity() != SCardError.Success)
                {
                    _cardContext.Establish(SCardScope.System);
                    _logger.LogInformation("PCSC context re-established, current status: {CardContextValidity}.", _cardContext.CheckValidity());
                }

                _readers = _cardContext.GetReaders();
                if (_readers == null || _readers.Length == 0)
                {
                    throw new Exception("No PCSC reader found.");
                }

                _currentReaderIndex = _configuration.Reader;
                if (_currentReaderIndex >= 0)
                {
                    var readerIndex = _currentReaderIndex;
                    if (InitalizeCard(_currentReaderIndex, _configuration.SerialNumber))
                    {
                        return;
                    }
                    else
                    {
                        throw new Exception($"Reader {_readers[readerIndex]} ({readerIndex}) initialization failed");
                    }
                }
                else if (!string.IsNullOrEmpty(_configuration.SerialNumber))
                {
                    for (var i = 0; i < _readers.Length; i++)
                    {
                        if (InitalizeCard(i, _configuration.SerialNumber))
                        {
                            return;
                        }
                    }
                    throw new Exception($"No smartcard with the serial number '{_configuration.SerialNumber}' found.");
                }
                else
                {
                    throw new ArgumentException("No reader number and no certificate serial provided, initialization failed. Please either set the SerialNumber or Reader property of the SCU.");
                }

            });           
        }

        private bool InitalizeCard(int readerIndex, string? serialnumber = null)
        {
            if (readerIndex < 0 || _readers == null || _readers.Length <= readerIndex)
            {
                throw new ArgumentException($"Reader index set to {readerIndex}, but only {_readers?.Length} readers are connected to the system. If you've manually set the Reader property of the SCU, please not that this index is 0-based (i.e. the first reader is addressed by the value 0).");
            }

            serialnumber = serialnumber?.Trim();

            if (string.IsNullOrWhiteSpace(serialnumber))
            {
                _logger.LogTrace("Searching for any card on reader with index {ReaderIndex}", readerIndex);
            }
            else
            {
                _logger.LogTrace("Searching card with serialnumber {Serialnumber} on reader with index {ReaderIndex}", serialnumber, readerIndex);
            }
            return _lockHelper.ExecuteReaderCommandLocked(_currentReaderIndex, Operation.InitalizeCard, () =>
            {
                try
                {
                    _cardReader = new SCardReader(_cardContext);
                    if (_configuration.Shared)
                    {
                        _isoReader = new IsoReader(_cardReader, _readers[readerIndex], SCardShareMode.Shared, SCardProtocol.Any, true);
                    }
                    else
                    {
                        _isoReader = new IsoReader(_cardReader, _readers[readerIndex], SCardShareMode.Exclusive, SCardProtocol.Any, true);
                    }

                    _card = _cardServiceFactory.CreateCardService(_cardReader, _isoReader);
                    _currentReaderIndex = readerIndex;
                    _currentReaderMode = _configuration.Shared ? ReaderMode.SharedOpen : ReaderMode.ExclusiveOpen;
                    if (!_card.CheckApplication())
                    {
                        throw new Exception("Applicaton not found.");
                    }

                    _certificate = _card.ReadCertificates();
                    _logger.LogTrace("ReadCertificate result: {Certificate}", BitConverter.ToString(_certificate));

                    var signature = _card.Sign(Guid.Empty.ToByteArray(), true);
                    _logger.LogTrace("Sign result: {Signature}", BitConverter.ToString(signature));

                    if (_configuration.VerifySignature && !Verify(Guid.Empty.ToByteArray(), signature))
                    {
                        throw new Exception("Signature verification failed.");
                    }

                    if (string.IsNullOrWhiteSpace(serialnumber) || CertificateHelpers.CompareSerialNumbers(_certificate, serialnumber))
                    {
                        _logger.LogInformation("Using reader {ReaderName} at index {ReaderIndex}.", _readers[_currentReaderIndex], _currentReaderIndex);
                        return true;
                    }
                    else if (!string.IsNullOrWhiteSpace(serialnumber) && !CertificateHelpers.CompareSerialNumbers(_certificate, serialnumber))
                    {
                        _logger.LogDebug("Serial number of reader {ReaderName} at index {ReaderIndex} did not match the specified serial '{SerialNumber}'.", _readers[_currentReaderIndex], _currentReaderIndex, serialnumber);
                    }
                }
                catch (Exception x)
                {
                    _logger.LogError(x, "An error occurred while trying to initialize the card (index: {ReaderIndex}, serial number: {Seruial}", readerIndex, serialnumber);
                }
                return false;

            });
        }

        private bool Verify(byte[] data, byte[] signature)
        {
            try
            {
                if (verifier == null)
                {
                    var x509cert = new Org.BouncyCastle.X509.X509CertificateParser().ReadCertificate(_certificate);
                    var x509pubkey = (Org.BouncyCastle.Crypto.Parameters.ECPublicKeyParameters) x509cert.GetPublicKey();
                    verifier = Org.BouncyCastle.Security.SignerUtilities.GetSigner("SHA-256withECDSA");
                    verifier.Init(false, x509pubkey);
                }

                // http://crypto.stackexchange.com/questions/1795/how-can-i-convert-a-der-ecdsa-signature-to-asn-1
                var dersigature = new byte[72];
                dersigature[0] = 0x30;
                dersigature[1] = 70; // data length
                dersigature[2] = 0x02; // element start
                dersigature[3] = 33; // length is always 32 + 1
                dersigature[4] = 0x0; // 0 for negative numbers
                Array.Copy(signature, 0, dersigature, 5, 32); // r

                dersigature[37] = 0x02; // element start
                dersigature[38] = 33; // length is always 32 + 1
                dersigature[39] = 0; // 0 for negative numbers
                Array.Copy(signature, 32, dersigature, 40, 32); // s

                verifier.Reset();
                verifier.BlockUpdate(data, 0, data.Length);

                return verifier.VerifySignature(dersigature);

            }
            catch (Exception x)
            {
                _logger.LogError(x, "An unexpected error occurred while trying to verify signed data.");
            }

            return false;
        }

        public Task<EchoResponse> EchoAsync(EchoRequest echoRequest) => Task.FromResult(new EchoResponse { Message = Echo(echoRequest.Message) });

        public string Echo(string message) => message;

        public IAsyncResult BeginEcho(string message, AsyncCallback callback, object state)
        {
            var d = new Echo_Delegate(Echo);
            var r = d.BeginInvoke(message, callback, d);
            return r;
        }

        public string EndEcho(IAsyncResult result)
        {
            var d = (Echo_Delegate) result.AsyncState;
            return d.EndInvoke(result);
        }

        public Task<CertificateResponse> CertificateAsync() => Task.FromResult(new CertificateResponse { Certificate = Certificate() });

        public byte[] Certificate() => _currentReaderMode == ReaderMode.DisabledOrUnknown ? null : _certificate;

        public IAsyncResult BeginCertificate(AsyncCallback callback, object state)
        {
            var d = new Certificate_Delegate(Certificate);
            var r = d.BeginInvoke(callback, d);
            return r;
        }

        public byte[] EndCertificate(IAsyncResult result)
        {
            var d = (Certificate_Delegate) result.AsyncState;
            return d.EndInvoke(result);
        }

        public Task<ZdaResponse> ZdaAsync() => Task.FromResult(new ZdaResponse { ZDA = ZDA() });

        public string ZDA() => _currentReaderMode == ReaderMode.DisabledOrUnknown ? null : ZDA_NAME;

        public IAsyncResult BeginZDA(AsyncCallback callback, object state)
        {
            var d = new ZDA_Delegate(ZDA);
            var r = d.BeginInvoke(callback, d);
            return r;
        }

        public string EndZDA(IAsyncResult result)
        {
            var d = (ZDA_Delegate) result.AsyncState;
            return d.EndInvoke(result);
        }

        public Task<SignResponse> SignAsync(SignRequest signRequest) => Task.FromResult(new SignResponse { SignedData = Sign(signRequest.Data) });

        public byte[] Sign(byte[] data)
        {
            try
            {
                _lockHelper.ExecuteReaderCommandLocked(_currentReaderIndex, Operation.WaitUntilReaderIsAvailable, () => true);
            }
            catch (Exception)
            {
                return null;
            }
            
            if (_currentReaderMode == ReaderMode.DisabledOrUnknown)
            {
                return null;
            }
            return _lockHelper.ExecuteReaderCommandLocked(_currentReaderIndex,Operation.Sign, () =>
            {
                var signature = _card.Sign(data, _configuration.ApduSelect);
                return _configuration.VerifySignature && !Verify(data, signature)
                    ? throw new Exception("Signature verficiation failed.")
                    : signature;
            });
        }

        public IAsyncResult BeginSign(byte[] data, AsyncCallback callback, object state)
        {
            var d = new Sign_Delegate(Sign);
            var r = d.BeginInvoke(data, callback, d);
            return r;
        }

        public byte[] EndSign(IAsyncResult result)
        {
            var d = (Sign_Delegate) result.AsyncState;
            return d.EndInvoke(result);
        }

        public void Dispose()
        {
            _watchdogTimer?.Stop();
            _watchdogTimer?.Dispose();
            _cardContext?.Dispose();
            _isoReader?.Dispose();
        }
    }
}
