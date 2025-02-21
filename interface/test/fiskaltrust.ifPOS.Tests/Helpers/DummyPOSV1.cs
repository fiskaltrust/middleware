using fiskaltrust.ifPOS.v1;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace fiskaltrust.ifPOS.Tests.Helpers
{
    public class DummyPOSV1 : DummyPOS, IPOS
    {
        private Task<T> FromResult<T>(T result)
        {
            return Task.Factory.StartNew(() => result);
        }

        public Task<ReceiptResponse> SignAsync(ReceiptRequest request)
        {
            return FromResult(new ReceiptResponse
            {
                ftQueueID = request.ftQueueID
            });
        }

        public Task<EchoResponse> EchoAsync(EchoRequest message)
        {
            return FromResult(new EchoResponse
            {
                Message = message.Message
            });
        }

#if STREAMING

        public async IAsyncEnumerable<JournalResponse> JournalAsync(JournalRequest request)
        {
            var filePath = "TestData.json";
            var lotsOfNumbers = Enumerable.Range(0, 100);
            var data = JsonConvert.SerializeObject(lotsOfNumbers);
            File.WriteAllText(filePath, data);

            var chunkSize = 4096;
            byte[] buffer = new byte[chunkSize];
            int bytesRead;
            using (var stream = File.OpenRead(filePath))
            {
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    yield return new JournalResponse
                    {
                        Chunk = buffer.ToList()
                    };
                    buffer = new byte[chunkSize];
                }
            }
        }

        public static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

#endif
    }
}
