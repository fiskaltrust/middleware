using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Exceptions;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Helpers;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File.Models.Parameters;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File.Native;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Models;
using Microsoft.Extensions.Logging;
using static fiskaltrust.Middleware.SCU.DE.CryptoVision.Models.Commands.TransportAdapterCommands;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File
{
    public class MassStorageClassTransportAdapter : ITseTransportAdapter
    {
        private const int TseIoWriteBlockSize = 0x0200;  // 512 byte block size for aligned write of TSE-IO.bin content
        private const int TseIoReadBlockSize = 0x2000;   // 8k block size for aligned read of TSE-IO.bin content
        private const string TseIoHeaderBase64 = "QWRWYW5jRUQgU2VDdVJlIFNEL01NQyBDQXJkAQ==";  // uint8_t MSC_HEADER[] = { (uint8_t) 0x41, (uint8_t) 0x64, (uint8_t) 0x56, (uint8_t) 0x61, (uint8_t) 0x6e, (uint8_t) 0x63, (uint8_t) 0x45, (uint8_t) 0x44, (uint8_t) 0x20, (uint8_t) 0x53, (uint8_t) 0x65, (uint8_t) 0x43, (uint8_t) 0x75, (uint8_t) 0x52, (uint8_t) 0x65, (uint8_t) 0x20, (uint8_t) 0x53, (uint8_t) 0x44, (uint8_t) 0x2f, (uint8_t) 0x4d, (uint8_t) 0x4d, (uint8_t) 0x43, (uint8_t) 0x20, (uint8_t) 0x43, (uint8_t) 0x41, (uint8_t) 0x72, (uint8_t) 0x64, (uint8_t) 0x01 };
        private const string TseIoHeaderEmptyBase64 = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==";
        private const string TseIoRandomTokenBase64 = "3q2+7w==";  // uint8_t MSC_DEADBEEF[] = { (uint8_t) 0xde, (uint8_t) 0xad, (uint8_t) 0xbe, (uint8_t) 0xef };
        private const int HwLockDefaultTimeout = 5000;

        private static readonly byte[] _tseIoHeaderBytes = Convert.FromBase64String(TseIoHeaderBase64);
        private static readonly byte[] _tseIoRandomTokenBytes = Convert.FromBase64String(TseIoRandomTokenBase64);
        private readonly int _hwLockTimeout = HwLockDefaultTimeout;

        private readonly string _tseIoPath;
        private readonly ILogger<MassStorageClassTransportAdapter> _logger;
        private readonly IOsFileIo _osFileIo;
        private readonly int _tseIoTimeout;
        private readonly int _tseIoReadDelay;
        private readonly bool _retryOnEmptyResponse;
        private readonly SemaphoreSlim hwSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim fileHandlingSemaphore = new SemaphoreSlim(1, 1);

        private bool _fragmentedReadInProgress = false;
        private bool _disposed;

        public MassStorageClassTransportAdapter(ILogger<MassStorageClassTransportAdapter> logger, IOsFileIo osFileIo, CryptoVisionConfiguration configuration)
        {
            _tseIoPath = GetTseIoPath(configuration);
            _logger = logger;
            _osFileIo = osFileIo;
            _tseIoTimeout = configuration.TseIOTimeout;
            _tseIoReadDelay = configuration.TseIOReadDelayMs;
            _retryOnEmptyResponse = configuration.RetryOnEmptyResponse;
        }

        private string GetTseIoPath(CryptoVisionConfiguration configuration)
        {
            var devicePath = configuration.DevicePath;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && devicePath.EndsWith(":"))
            {
                devicePath += "\\";
            }
            return Path.Combine(devicePath, Constants.TseIoFile);
        }

        public void OpenFile() => LockingHelper.PerformWithLock(fileHandlingSemaphore, () => _osFileIo.OpenFile(_tseIoPath), HwLockDefaultTimeout);

        public void CloseFile() => LockingHelper.PerformWithLock(fileHandlingSemaphore, () => _osFileIo.CloseFile(), HwLockDefaultTimeout);

        public void ReopenFile()
        {
            CloseFile();
            OpenFile();
        }

        public async Task<List<ITseData>> ExecuteAsync(ITseCommand tseCommand)
        {
            var isLocked = false;
            try
            {
                isLocked = await hwSemaphore.WaitAsync(_hwLockTimeout);
                if (!isLocked)
                {
                    throw new Exception($"Unable to perform lock after {_hwLockTimeout} ms");
                }
                if (!_osFileIo.IsOpen)
                {
                    ReopenFile();
                }
                Write(tseCommand.GetCommandDataBytes(), tseCommand.ResponseModeBytes, _tseIoRandomTokenBytes);

                var resultEnumerable = await ReadTseDataAsync(_tseIoRandomTokenBytes);
                _fragmentedReadInProgress = false;
                return resultEnumerable;
            }
            catch
            {
                if (_fragmentedReadInProgress)
                {
                    try
                    {
                        var cancelTseCommand = new CancelReadFragmentsTseCommand();
                        Write(cancelTseCommand.GetCommandDataBytes(), cancelTseCommand.ResponseModeBytes, _tseIoRandomTokenBytes);
                    }
                    catch { }
                    finally
                    {
                        _fragmentedReadInProgress = false;
                    }
                }
                throw;
            }
            finally
            {
                if (isLocked)
                {
                    hwSemaphore.Release();
                }
            }
        }

        private void Write(byte[] commandDataBytes, byte[] responseModeBytes, byte[] randomTokenBytes)
        {
            if (commandDataBytes.Length > TseIoReadBlockSize - 32)
            {
                throw new ArgumentException("Write block size to big, max 8k", nameof(commandDataBytes));
            }
            if (responseModeBytes.Length != 2)
            {
                //response-mode need to be exact 2bytes
                throw new ArgumentException("Response-mode need to be exact 2 bytes.", nameof(responseModeBytes));
            }
            if (randomTokenBytes.Length != 4)
            {
                throw new ArgumentException("Random-token need to be exact 4 bytes.", nameof(randomTokenBytes));
            }

            // 32 byte header
            // 2 byte commanddata len
            // 2 byte response mode
            // commanddata
            // padding to full block
            var blocksToWrite = ((32 + 2 + 2 + commandDataBytes.Length - 1) / TseIoWriteBlockSize) + 1;

            var buffer = new byte[blocksToWrite * TseIoWriteBlockSize];

            // header (offset 0, length 32)
            Buffer.BlockCopy(_tseIoHeaderBytes, 0, buffer, 0, _tseIoHeaderBytes.Length);
            // random-token (offset 28, length 4)
            Buffer.BlockCopy(randomTokenBytes, 0, buffer, 28, randomTokenBytes.Length);

            // commanddata length
            Buffer.BlockCopy(((ushort) commandDataBytes.Length).ToBytes(), 0, buffer, 32, 2);
            // response mode
            Buffer.BlockCopy(responseModeBytes, 0, buffer, 34, responseModeBytes.Length);
            // commanddata
            Buffer.BlockCopy(commandDataBytes, 0, buffer, 36, commandDataBytes.Length);

            _osFileIo.SeekBegin();
            _osFileIo.Write(buffer, blocksToWrite, TseIoWriteBlockSize);
        }

        private async Task<List<ITseData>> ReadTseDataAsync(byte[] randomTokenBytes, bool exportData = false)
        {
            var maxTimeStamp = DateTimeOffset.UtcNow.AddMilliseconds(_tseIoTimeout);
            var randomTokenBase64 = Convert.ToBase64String(randomTokenBytes);

            do
            {
                await Task.Delay(_tseIoReadDelay);

                _osFileIo.SeekBegin();
                var readBuffer = _osFileIo.Read(1, TseIoReadBlockSize);

                var readBufferTokenBase64 = Convert.ToBase64String(readBuffer.Skip(28).Take(4).ToArray());
                if (readBufferTokenBase64 == randomTokenBase64)
                {
                    if (DateTimeOffset.UtcNow > maxTimeStamp)
                    {
                        throw new TimeoutException($"The timeout of {_tseIoTimeout} for reading data from the TSE has expired.");
                    }
                    continue;
                }

                var readBufferHeaderBase64 = Convert.ToBase64String(readBuffer.Take(28).ToArray());
                if (TseIoHeaderBase64 != readBufferHeaderBase64)
                {
                    if (_retryOnEmptyResponse && readBufferHeaderBase64 == TseIoHeaderEmptyBase64 && DateTimeOffset.UtcNow < maxTimeStamp)
                    {
                        _logger.LogWarning("Received an empty header from the TSE, retrying...");
                        continue;
                    }

                    throw new Exception($"Response headers didn´t match. Expected <{TseIoHeaderBase64}>, but instead got <{readBufferHeaderBase64 }>.");
                }

                var responseDataLength = readBuffer.ToUInt16(32);
                if (responseDataLength == 0xFFFF)
                {
                    if (DateTimeOffset.UtcNow > maxTimeStamp)
                    {
                        throw new TimeoutException($"The timeout of {_tseIoTimeout} for reading data from the TSE has expired.");
                    }
                    continue;
                }
                if (responseDataLength == 0xFF45)
                {
                    throw new CryptoVisionProxyException(SeResult.ErrorTSETimeout);
                }
                if ((responseDataLength & 0xFF00) == 0xFF00)
                {
                    // flash-controller detected other unexpected errors
                    throw new CryptoVisionProxyException(SeResult.ErrorClassSD);
                }

                if (responseDataLength > readBuffer.Length - 32 - 2)
                {
                    // response data size exceeds readed size
                    throw new Exception($"Response data size exceeds read limit. Expected to be smaller than <{responseDataLength}>, but instead got {readBuffer.Length - 32 - 2}.");
                }

                if (responseDataLength == 0)
                {
                    // no data provided, e.g. Enabling/Disabling Suspend Mode
                    return new List<ITseData>();
                }

                var responseData = readBuffer.Skip(34).Take(responseDataLength).ToArray();

                if (exportData)
                {
                    var rawData = new TseRawData();
                    rawData.Write(responseData);
                    return new List<ITseData> { rawData };
                }

                var resultLength = responseData.ToUInt16();

                if (resultLength == 0)
                {
                    // no data to respond
                    return new List<ITseData>();
                }
                else if (resultLength < 0x8000)
                {
                    var responseBytes = new List<byte>();
                    if (resultLength > responseData.Length - 2)
                    {
                        _fragmentedReadInProgress = true;
                        responseBytes.AddRange(responseData);
                        var readNextCommand = new ReadNextFragmentTseCommand();
                        Write(readNextCommand.GetCommandDataBytes(), readNextCommand.ResponseModeBytes, _tseIoRandomTokenBytes);
                        foreach (var item in await ReadTseDataAsync(_tseIoRandomTokenBytes, true))
                        {
                            responseBytes.AddRange(item.DataBytes);
                        }
                        var readNext = await ReadNextCommandAsync(readNextCommand, resultLength, responseBytes.Count - 2);
                        responseBytes.AddRange(readNext);
                    }
                    else
                    {
                        responseBytes.AddRange(responseData);
                    }
                    var resultData = new List<ITseData>();
                    var parameterOffset = 2;
                    while (parameterOffset < resultLength)
                    {
                        var tseParameter = responseBytes.ToArray().ToTseParameter(parameterOffset);
                        parameterOffset += 1 + 2 + tseParameter.DataLength;
                        resultData.Add(tseParameter);
                    }
                    return resultData;
                }
                else if (resultLength == 0x9000)
                {
                    _fragmentedReadInProgress = true;
                    // response tar-file
                    // fragmented response data, multiple data blocks
                    var totalExportLength = responseData.ToInt64(2);
                    if (totalExportLength < 0)
                    {
                        // total length should be 63bit. seen in v2.2.0 c-implementation
                        throw new CryptoVisionProxyException(SeResult.ErrorTSEResponseDataInvalid);
                    }

                    var responseBytes = new List<byte>();
                    long maximumCurrentDataLength = responseData.Length - 10;
                    if (totalExportLength <= maximumCurrentDataLength)
                    {
                        // total export is in first response data block
                        responseBytes.AddRange(responseData);
                    }
                    else
                    {
                        //total export is fragmented over multiple response data blocks
                        totalExportLength -= responseData.Length - 10;
                        responseBytes.AddRange(responseData);

                        while (totalExportLength > 0)
                        {
                            var readNextCommand = new ReadNextFragmentTseCommand();
                            Write(readNextCommand.GetCommandDataBytes(), readNextCommand.ResponseModeBytes, _tseIoRandomTokenBytes);
                            foreach (var item in await ReadTseDataAsync(_tseIoRandomTokenBytes, true))
                            {
                                totalExportLength -= item.DataLength;
                                responseBytes.AddRange(item.DataBytes);
                            }
                        }
                    }

                    //1024 byte tar-file padding.
                    responseBytes.AddRange(new byte[1024]);
                    var resultingBytes = responseBytes.ToArray();
                    var exportDataBytes = resultingBytes.Skip(10).ToArray();
                    var rawData = new TseRawData();
                    rawData.Write(exportDataBytes);
                    return new List<ITseData> { rawData };
                }
                else if ((resultLength & 0x8F00) == 0x8000)
                {
                    // error code
                    throw new CryptoVisionProxyException(MapIoResult(resultLength));
                }
                else
                {
                    // unknown respose
                    throw new CryptoVisionProxyException(SeResult.ErrorTSEUnknownError);
                }
            } while (true);
        }

        private async Task<List<byte>> ReadNextCommandAsync(ReadNextFragmentTseCommand readNextCommand,ushort resultLength, int responseBytesCount)
        {
            Write(readNextCommand.GetCommandDataBytes(), readNextCommand.ResponseModeBytes, _tseIoRandomTokenBytes);
            var readItems = await ReadTseDataAsync(_tseIoRandomTokenBytes, true);
            var responseBytes = new List<byte>();
            if (responseBytesCount >= resultLength)
            {
                return new List<byte>();
            }
            foreach (var item in readItems)
            {
                responseBytes.AddRange(item.DataBytes);
            }
            var readNext = await ReadNextCommandAsync(readNextCommand, resultLength, responseBytesCount+ responseBytes.Count);
            responseBytes.AddRange(readNext);
            return responseBytes;
        }

        private SeResult MapIoResult(ushort result)
        {
            const ushort SE_ERROR_TIMEOUT = 0x8100;
            const ushort SE_ERROR_STREAM_WRITE = 0x8101;
            const ushort SE_BUFFER_TOO_SMALL = 0x8102;
            const ushort SE_ALLOCATION_FAILED = 0x8103;
            const ushort SE_ERROR_CALLBACK = 0x8104;

            return result switch
            {
                0x8000 => SeResult.ErrorSECommunicationFailed,
                0x8001 => SeResult.ErrorTSECommandDataInvalid,
                0x8002 => SeResult.ErrorTSEResponseDataInvalid,
                0x8003 => SeResult.ErrorSigningSystemOperationDataFailed,
                0x8004 => SeResult.ErrorRetrieveLogMessageFailed,
                0x8005 => SeResult.ErrorStorageFailure,
                0x8006 => SeResult.ErrorSecureElementDisabled,
                0x8007 => SeResult.ErrorUserNotAuthorized,
                0x8008 => SeResult.ErrorUserNotAuthenticated,
                0x8009 => SeResult.ErrorSeApiNotInitialized,
                0x800A => SeResult.ErrorUpdateTimeFailed,
                0x800B => SeResult.ErrorUserIdNotManaged,
                0x800C => SeResult.ErrorStartTransactionFailed,
                0x800D => SeResult.ErrorCertificateExpired,
                0x800E => SeResult.ErrorNoTransaction,
                0x800F => SeResult.ErrorUpdateTransactionFailed,
                0x8010 => SeResult.ErrorFinishTransactionFailed,
                0x8011 => SeResult.ErrorTimeNotSet,
                0x8012 => SeResult.ErrorNoERS,
                0x8013 => SeResult.ErrorNoKey,
                0x8014 => SeResult.ErrorSeApiNotDeactivated,
                0x8015 => SeResult.ErrorNoDataAvailable,
                0x8016 => SeResult.ErrorTooManyRecords,
                0x8017 => SeResult.ErrorUnexportedStoredData,
                0x8018 => SeResult.ErrorParameterMismatch,
                0x8019 => SeResult.ErrorIdNotFound,
                0x801A => SeResult.ErrorTransactionNumberNotFound,
                0x801B => SeResult.ErrorSeApiDeactivated,
                0x801C => SeResult.ErrorTransport,
                0x801D => SeResult.ErrorNoStartup,
                0x801E => SeResult.ErrorNoStorage,
                SE_ERROR_TIMEOUT => SeResult.ErrorTSETimeout,
                SE_ERROR_STREAM_WRITE => SeResult.ErrorStreamWrite,
                SE_BUFFER_TOO_SMALL => SeResult.ErrorBufferTooSmall,
                SE_ALLOCATION_FAILED => SeResult.ErrorAllocationfailed,
                SE_ERROR_CALLBACK => SeResult.ErrorCallback,
                _ => SeResult.ErrorTSEUnknownError,
            };
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                CloseFile();
                _osFileIo?.Dispose();
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}