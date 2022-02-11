using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models.Enums;

namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Helpers
{
    public static class TseResultHelper
    {
        public static TseResult CreateTseResult(Guid requestId, byte[] readBuffer)
        {
            var readBufferList = readBuffer.ToList();
            var responseCommandMarker = GetMarker(readBufferList);
            var responseRequestId = GetRequestId(readBufferList);
            var parameters = GetValuesForParameters(GetParameters(readBufferList).ToList());

            var tseResult = new TseResult
            {
                Command = responseCommandMarker,
                Parameters = parameters,
                RequestId = responseRequestId.Value
            };
            if (tseResult.RequestId != requestId)
            {
                throw new Exception($"Something weird happend. The expected requestid is {requestId} but the returned was {tseResult.RequestId}");
            }
            return tseResult;
        }

        public static TseResult CreateTseResult(byte[] readBuffer)
        {
            var readBufferList = readBuffer.ToList();
            var responseCommandMarker = GetMarker(readBufferList);
            var responseRequestId = GetRequestId(readBufferList);
            var parameters = GetValuesForParameters(GetParameters(readBufferList).ToList());

            var tseResult = new TseResult
            {
                Command = responseCommandMarker,
                Parameters = parameters,
                RequestId = responseRequestId.Value
            };
            return tseResult;
        }

        public static TseResult CreateTseResult(byte[] readBuffer, int expectedParameters)
        {
            var readBufferList = readBuffer.ToList();
            var responseCommandMarker = GetMarker(readBufferList);
            var responseRequestId = GetRequestId(readBufferList);
            var parameters = GetValuesForParameters(GetParameters(readBufferList).ToList(), expectedParameters);

            var tseResult = new TseResult
            {
                Command = responseCommandMarker,
                Parameters = parameters,
                RequestId = responseRequestId.Value
            };
            return tseResult;
        }

        public static DieboldNixdorfCommand GetMarker(List<byte> buffer) => (DieboldNixdorfCommand) int.Parse(Encoding.UTF8.GetString(new byte[] { buffer[2], buffer[3], buffer[4] }));

        public static List<List<byte>> GetValuesForParameters(List<byte> parametersBuffer)
        {
            var parameterBytes = new List<List<byte>>();
            var paramIndizes = new List<int>();
            for (var i = 0; i < parametersBuffer.Count; i++)
            {
                if (parametersBuffer[i] == 0x92)
                {
                    paramIndizes.Add(i);
                }
            }

            for (var i = 0; i < paramIndizes.Count; i++)
            {
                var startIndex = paramIndizes[i] + 1;
                var endIndex = parametersBuffer.Count;
                if (i < paramIndizes.Count - 1)
                {
                    endIndex = paramIndizes[i + 1] - 1;
                }
                var chunk = parametersBuffer.Skip(startIndex).Take(endIndex - startIndex).ToList();
                parameterBytes.Add(chunk);
            }
            return parameterBytes;
        }

        public static List<List<byte>> GetValuesForParameters(List<byte> parametersBuffer, int expectedParameters)
        {
            var parameterBytes = new List<List<byte>>();
            var paramIndizes = new List<int>();
            for (var i = 0; i < parametersBuffer.Count; i++)
            {
                if (paramIndizes.Count == expectedParameters)
                {
                    break;
                }
                if (parametersBuffer[i] == 0x92)
                {
                    paramIndizes.Add(i);
                }
            }

            for (var i = 0; i < paramIndizes.Count; i++)
            {
                var startIndex = paramIndizes[i] + 1;
                var endIndex = parametersBuffer.Count;
                if (i < paramIndizes.Count - 1)
                {
                    endIndex = paramIndizes[i + 1] - 1;
                }
                var chunk = parametersBuffer.Skip(startIndex).Take(endIndex - startIndex).ToList();
                parameterBytes.Add(chunk);
            }
            return parameterBytes;
        }

        public static byte[] GetParameters(List<byte> buffer)
        {
            var parametersBuffer = new List<byte>();
            var sequenceStartMarker = buffer.IndexOf(0x92);
            if (sequenceStartMarker == -1)
            {
                return Array.Empty<byte>();
            }
            var requestIdMarkerIndex = buffer.LastIndexOf(0x99);
            int i;
            for (i = sequenceStartMarker - 1; i < requestIdMarkerIndex - 1; i++)
            {
                parametersBuffer.Add(buffer[i]);
            }
            return parametersBuffer.ToArray();
        }

        public static Guid? GetRequestId(List<byte> buffer)
        {
            var requestIdBytes = new List<byte>();
            var requestIdMarkerIndex = buffer.LastIndexOf(0x99);
            var responseEndMarkerIndex = buffer.LastIndexOf(0x9F);

            if (requestIdMarkerIndex == -1 || responseEndMarkerIndex == -1)
            {
                return null;
            }

            for (var i = requestIdMarkerIndex + 1; i < responseEndMarkerIndex - 1; i++)
            {
                requestIdBytes.Add(buffer[i]);
            }
            return Guid.Parse(Encoding.UTF8.GetString(requestIdBytes.ToArray()));
        }
    }
}