using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Logs.Models;
using fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Tlv;
using fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Tlv.Models;
using Org.BouncyCastle.Asn1;
using SharpCompress.Archives.Tar;

namespace fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Logs
{
    public static class LogParser
    {
        public static List<Tuple<byte[], string>> GetCertificatesFromByteArray(byte[] data)
        {
            var certificates = new List<Tuple<byte[], string>>();
            using (var stream = new MemoryStream(data))
            {
                certificates = GetCertificatesFromTarStream(stream);
            }

            return certificates;
        }

        public static List<Tuple<byte[], string>> GetCertificatesFromTarStream(Stream stream)
        {
            var certificates = new List<Tuple<byte[], string>>();
            using (var reader = TarArchive.Open(stream))
            {
                foreach (var entry in reader.Entries.ToList().Where(entry => Path.GetExtension(entry.Key) == ".cer" || Path.GetExtension(entry.Key) == ".pem"))
                {
                    using (var entryStream = entry.OpenEntryStream())
                    using (var ms = new MemoryStream())
                    {
                        entryStream.CopyTo(ms);
                        certificates.Add(new Tuple<byte[], string>(ms.ToArray(), entry.Key));
                    }
                }
            }
            return certificates;
        }

        public static IEnumerable<LogMessage> GetLogsFromByteArray(byte[] data)
        {
            var logMessages = new List<LogMessage>();
            using (var stream = new MemoryStream(data))
            {
                logMessages = GetLogsFromTarStream(stream).ToList();
            }

            return logMessages;
        }

        public static IEnumerable<LogMessage> GetLogsFromTarStream(Stream stream)
        {
            using (var reader = TarArchive.Open(stream))
            {
                var result = new List<LogMessage>();
                foreach (var entry in reader.Entries.ToList().Where(entry => Path.GetExtension(entry.Key) == ".log"))
                {
                    try
                    {
                        using (var entryStream = entry.OpenEntryStream())
                        using (var ms = new MemoryStream())
                        {
                            entryStream.CopyTo(ms);
                            var logMessage = Parse(ms.ToArray());
                            logMessage.FileName = entry.Key;

                            result.Add(logMessage);
                        }
                    }
                    catch (Exception)
                    {
                        // TODO Fix parsing of DF FinishTransactions
                    }
                }

                return result;
            }
        }

        public static LogMessage Parse(byte[] log)
        {
            var tlvResult = TlvParser.ParseTlv(log);
            var parsedMessage = tlvResult[0].Children;
            var integerValues = parsedMessage.Where(x => x.Tag == 0x02).ToList();
            var octetStrings = parsedMessage.Where(x => x.Tag == 0x04).ToList();
            var message = CreateLogMessage(parsedMessage);
            message.RawData = log.ToList();
            message.Version = TlvParseHelpers.ParseInteger(integerValues[0].Value);
            message.SignaturCounter = (ulong)TlvParseHelpers.ParseInteger(integerValues[1].Value);
            message.SignatureAlgorithm = GetSignaturAlgorithm(parsedMessage.Single(x => x.Tag == 0x30).Children);
            message.SerialNumber = BitConverter.ToString(octetStrings[0].Value).Replace("-", string.Empty);
            message.SignaturValueBase64 = Convert.ToBase64String(octetStrings.Last().Value);

            if (integerValues.Count == 3)
            {
                message.LogTimeFormat = "unixTime";
                message.LogTime = TlvParseHelpers.ParseUnixTimeStamp(integerValues[2].Value);
            }
            else if (!parsedMessage.Any(x => x.Tag == 0x17))
            {
                message.LogTimeFormat = "generalizedTime";
                message.LogTime = TlvParseHelpers.ParseGeneralizedTime(parsedMessage.Single(x => x.Tag == 0x18).Value);
            }
            else
            {
                message.LogTimeFormat = "utcTime";
                message.LogTime = TlvParseHelpers.ParseUtcTime(parsedMessage.Single(x => x.Tag == 0x17).Value);
            }
            return message;
        }

        private static LogMessage CreateLogMessage(List<TlvRecord> parsedMessage)
        {
            var startChunk = parsedMessage.FindIndex(x => x.Tag == 0x80);
            var endChunk = parsedMessage.FindIndex(x => x.Tag == 0x04);
            var octetStrings = parsedMessage.Where(x => x.Tag == 0x04).ToList();
            var logMessageType = TlvParseHelpers.ParseObjectIdentifier(parsedMessage[1]);
            switch (logMessageType)
            {
                case OIDs.SYSTEM_LOGS:
                    return ParseAuditLogMessage(octetStrings);

                case OIDs.TRANSACTION_LOGS:
                    return ParseTransactionLogMessage(parsedMessage.Skip(startChunk).Take(endChunk - startChunk).ToList());

                case OIDs.AUDIT_LOGS:
                    return ParseSystemLogMessage(parsedMessage);

                default:
                    throw new NotSupportedException($"The logMessageType {BitConverter.ToString(parsedMessage[1].RawData)} is not supported.");
            }
        }

        private static SignatureAlgorithm GetSignaturAlgorithm(List<TlvRecord> tlvs)
        {
            var alrogithmOid = TlvParseHelpers.ParseObjectIdentifier(tlvs.Last(x => x.Tag == 0x06));
            var result = new Oid(alrogithmOid);
            return new SignatureAlgorithm
            {
                Oid = result.Value,
                Algorithm = SignatureAlgorithm.NameFromOid(alrogithmOid),
                Parameters = new List<string>() // TODO how to define parameters?
            };
        }

        public static IEnumerable<SignatureAlgorithm> GetSignaturAlgorithm(byte[] asn1EncodedOidArray)
        {
            foreach (var item in GetSignaturAlgorithmOid(asn1EncodedOidArray))
            {
                var oid = new Oid(item.ToString());
                yield return new SignatureAlgorithm
                {
                    Oid = oid.Value,
                    Algorithm = oid.FriendlyName ?? oid.Value,
                    Parameters = new List<string>()
                };
            }
        }

        public static IEnumerable<string> GetSignaturAlgorithmOid(byte[] asn1EncodedOidArray)
        {
            using (var asn1InputStream = new Asn1InputStream(asn1EncodedOidArray))
            {
                var derSequenceObject = (DerSequence) asn1InputStream.ReadObject();
                foreach (var item in derSequenceObject)
                {
                    yield return item.ToString();
                }
            }

        }

        private static LogMessage ParseAuditLogMessage(List<TlvRecord> octetStrings)
        {
            return new AuditLogMessage
            {
                SeAuditData = Convert.ToBase64String(octetStrings[1].Value)
            };
        }

        private static SystemLogMessage ParseSystemLogMessage(List<TlvRecord> data)
        {
            var message = new SystemLogMessage();

            var operationTypeElement = data.SingleOrDefault(x => x.Tag == 0x80);
            if (operationTypeElement != null)
            {
                message.OperationType = Encoding.UTF8.GetString(operationTypeElement.Value);
            }

            var systemOperationDataElement = data.SingleOrDefault(x => x.Tag == 0x81);
            if (systemOperationDataElement != null)
            {
                message.SystemOperationData = systemOperationDataElement.Value.ToList();
            }

            var additionalInternalDataElement = data.FirstOrDefault(x => x.Tag == 0x82);
            if (additionalInternalDataElement != null)
            {
                message.AdditionalInternalData = additionalInternalDataElement.Value.ToList();
            }

            return message;
        }

        private static TransactionLogMessage ParseTransactionLogMessage(List<TlvRecord> data)
        {
            var message = new TransactionLogMessage
            {
                OperationType = Encoding.UTF8.GetString(data.Single(x => x.Tag == 0x80).Value),
                ClientId = Encoding.UTF8.GetString(data.Single(x => x.Tag == 0x81).Value),
                ProcessDataBase64 = Convert.ToBase64String(data.Single(x => x.Tag == 0x82).Value),
                TransactionNumber = (ulong) TlvParseHelpers.ParseInteger(data.Single(x => x.Tag == 0x85).Value)
            };
            var processTypeElement = data.FirstOrDefault(x => x.Tag == 0x83);
            if (processTypeElement != null)
            {
                message.ProcessType = Encoding.UTF8.GetString(processTypeElement.Value);
            }

            var additionalExternalDataElement = data.FirstOrDefault(x => x.Tag == 0x84);
            if (additionalExternalDataElement != null)
            {
                message.AdditionalExternalData = additionalExternalDataElement.Value.ToList();
            }
            var additionalInternalDataElement = data.FirstOrDefault(x => x.Tag == 0x86);
            if (additionalInternalDataElement != null)
            {
                message.AdditionalInternalData = additionalInternalDataElement.Value.ToList();
            }
            return message;
        }
    }
}