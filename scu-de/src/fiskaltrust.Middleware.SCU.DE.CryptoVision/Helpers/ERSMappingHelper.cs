using System.Collections.Generic;
using System.Linq;
using System.Text;
using Org.BouncyCastle.Asn1;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Helpers
{
    public static class ERSMappingHelper
    {
        //TODO investigate if for all tse the same, investigate movement to tlv-parser
        public static List<string> ERSMappingsAsString(byte[] derEncodedMappingData)
        {
            try
            {
                return ReadClientsFromASN1(derEncodedMappingData);
            }
            catch
            {
                // 2020-11-16 SKE:  In the current version of the CryptoVision TSE (at least until 2.3.1) the returned ASN.1 
                //                  has a wrong length alignment. For this reason we are facing failing operations while trying to read the clients
                //                  and so we try to make sure that everything is readable by correcting the length.
                var correctedLength = derEncodedMappingData.Skip(3).Count();
                derEncodedMappingData[2] = (byte) correctedLength;

                var correctedTotalLength = derEncodedMappingData.Skip(1).Count();
                derEncodedMappingData[1] = (byte) correctedTotalLength;

                try
                {
                    return ReadClientsFromASN1(derEncodedMappingData);
                }
                catch
                {
                    return new List<string>();
                }
            }
        }

        private static List<string> ReadClientsFromASN1(byte[] derEncodedMappingData)
        {
            using var asn1InputStream = new Asn1InputStream(new System.IO.MemoryStream(derEncodedMappingData));
            if (!(asn1InputStream.ReadObject() is DerSequence derSequence))
            {
                return new List<string>();
            }

            var ersMappingsAsString = new List<string>();

            for (var i = 0; i < derSequence.Count; i++)
            {
                if (derSequence[i].ToAsn1Object() is DerSequence sequence && sequence.Count > 0)
                {
                    if (sequence[0] is DerOctetString ersOctetString)
                    {
                        ersMappingsAsString.Add(Encoding.ASCII.GetString(ersOctetString.GetOctets()));
                    }
                }
            }

            return ersMappingsAsString;
        }
    }
}
