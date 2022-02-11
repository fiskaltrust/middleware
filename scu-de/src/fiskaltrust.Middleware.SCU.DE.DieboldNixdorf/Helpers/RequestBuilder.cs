using System;
using System.Collections.Generic;
using System.Text;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models.Enums;

namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf
{
    public static class RequestHelpers
    {
        public static byte ESC = 0x1B;
        public static byte[] MFSC_RequestStartMarker = new byte[] { ESC, 0x90 };
        public static byte[] MFSR_ResponseStartMarker = new byte[] { ESC, 0x91 };
        public static byte[] MFP_ParametersMarker = new byte[] { ESC, 0x92 };
        public static byte[] MFIDR_RequestIdMarker = new byte[] { ESC, 0x99 };
        public static byte[] MFRN_NonePositiveAnswersMarker = new byte[] { ESC, 0x9C };
        public static byte[] MFRA_PositiveAnswersMarker = new byte[] { ESC, 0x9D };
        public static byte[] MFRB_PositiveAnswersInBufferMarker = new byte[] { ESC, 0x9E };
        public static byte[] MFE_SequenceEndMarker = new byte[] { ESC, 0x9F };

        public static (byte[] request, Guid requestId) BuildRequestWithGetCommandResponseAnswer(DieboldNixdorfCommand command, int bufferIdentifier, List<string> parameters)
        {
            var request = new List<byte>();
            request.AddRange(MFSC_RequestStartMarker);
            request.AddRange(Encoding.ASCII.GetBytes(((int) command).ToString("D3")));
            if (parameters != null && parameters.Count > 0)
            {
                foreach (var parameter in parameters)
                {
                    request.AddRange(MFP_ParametersMarker);
                    request.AddRange(Encoding.ASCII.GetBytes(parameter));
                }
            }
            var requestId = Guid.NewGuid();
            request.AddRange(MFIDR_RequestIdMarker);
            request.AddRange(Encoding.ASCII.GetBytes(requestId.ToString()));

            request.AddRange(MFRB_PositiveAnswersInBufferMarker);

            request.AddRange(Encoding.ASCII.GetBytes(bufferIdentifier.ToString()));

            request.AddRange(MFE_SequenceEndMarker);
            return (request.ToArray(), requestId);
        }

        public static (byte[] request, Guid requestId) BuildRequest(DieboldNixdorfCommand command, List<string> parameters)
        {
            var request = new List<byte>();
            request.AddRange(MFSC_RequestStartMarker);
            request.AddRange(Encoding.ASCII.GetBytes(((int) command).ToString("D3")));

            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    request.AddRange(MFP_ParametersMarker);
                    request.AddRange(Encoding.ASCII.GetBytes(parameter));
                }
            }

            var requestId = Guid.NewGuid();
            request.AddRange(MFIDR_RequestIdMarker);
            request.AddRange(Encoding.ASCII.GetBytes(requestId.ToString()));

            request.AddRange(MFRA_PositiveAnswersMarker);

            request.AddRange(MFE_SequenceEndMarker);
            return (request.ToArray(), requestId);
        }
    }
}