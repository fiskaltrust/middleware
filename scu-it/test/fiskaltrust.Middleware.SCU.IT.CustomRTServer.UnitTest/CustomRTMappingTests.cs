using System;
using System.IO;
using fiskaltrust.ifPOS.v1;
using FluentAssertions;
using FluentAssertions.Execution;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTServer.UnitTest
{
    public class CustomRTMappingTests
    {
        [Fact]
        public void GenerateTaxDataForReceiptRequest_Should_Map_Correctly()
        {
            var receiptRequest = ReceiptExamples.GetTakeAway_Delivery_Cash();

            var data = CustomRTServerMapping.GenerateTaxDataForReceiptRequest(receiptRequest);

            using (new AssertionScope())
            {
                data.Should().ContainSingle(x => x.vatvalue == 2200)
                    .Which.Should().BeEquivalentTo(new DocumentTaxData
                    {
                        vatvalue = 2200,
                        tax = 3985,
                        vatcode = "",
                        gross = 22100
                    });
                data.Should().ContainSingle(x => x.vatvalue == 1000)
                       .Which.Should().BeEquivalentTo(new DocumentTaxData
                       {
                           vatvalue = 1000,
                           tax = 973,
                           vatcode = "",
                           gross = 10700
                       });
                data.Should().ContainSingle(x => x.vatvalue == 500)
                    .Which.Should().BeEquivalentTo(new DocumentTaxData
                    {
                        vatvalue = 500,
                        tax = 419,
                        vatcode = "",
                        gross = 8800
                    });
                data.Should().ContainSingle(x => x.vatvalue == 400)
                    .Which.Should().BeEquivalentTo(new DocumentTaxData
                    {
                        vatvalue = 400,
                        tax = 346,
                        vatcode = "",
                        gross = 9000
                    });
                data.Should().ContainSingle(x => x.vatcode == "N4")
                  .Which.Should().BeEquivalentTo(new DocumentTaxData
                  {
                      vatvalue = 0,
                      tax = 0,
                      vatcode = "N4",
                      gross = 1000
                  });
                data.Should().ContainSingle(x => x.vatcode == "N3")
                  .Which.Should().BeEquivalentTo(new DocumentTaxData
                  {
                      vatvalue = 0,
                      tax = 0,
                      vatcode = "N3",
                      gross = 1000
                  });
                data.Should().ContainSingle(x => x.vatcode == "N2")
                     .Which.Should().BeEquivalentTo(new DocumentTaxData
                     {
                         vatvalue = 0,
                         tax = 0,
                         vatcode = "N2",
                         gross = 1000
                     });
                data.Should().ContainSingle(x => x.vatcode == "N1")
                   .Which.Should().BeEquivalentTo(new DocumentTaxData
                   {
                       vatvalue = 0,
                       tax = 0,
                       vatcode = "N1",
                       gross = 1000
                   });
                // RM??
                // AL?
            }
        }

        [Fact]
        public void Sales()
        {
            var allFiles = Directory.GetFiles("C:\\Users\\Stefan\\Documents\\GitHub\\middleware\\scu-it\\test\\fiskaltrust.Middleware.SCU.IT.CustomRTServer.UnitTest\\ReceiptCases\\Sales");
            foreach (var file in allFiles)
            {
                if (file.EndsWith("_rec.json"))
                {
                    continue;
                }

                var content = File.ReadAllText(file);
                content = content.Replace("{{current_moment}}", DateTime.UtcNow.ToString("o"));
                var receiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(content);

                foreach (var chargeItem in receiptRequest.cbChargeItems)
                {
                    chargeItem.VATAmount = Math.Round(chargeItem.Amount - (chargeItem.Amount / (1m + (chargeItem.VATRate / 100m))), 2, MidpointRounding.AwayFromZero);
                }

                var (_, fiscalDocument) = CustomRTServerMapping.GenerateFiscalDocument(receiptRequest, new Models.QueueIdentification
                {
                    CashUuId = "ske00003",
                    CashHmacKey = "123djfjasdfj",
                    LastDocNumber = 1,
                    LastZNumber = 1,
                    LastSignature = "asdf",
                    CurrentGrandTotal = 1,
                    RTServerSerialNumber = "SSS"
                });
                var filePath = Path.Combine($"C:\\Users\\Stefan\\Documents\\GitHub\\middleware\\scu-it\\test\\fiskaltrust.Middleware.SCU.IT.CustomRTServer.UnitTest\\ReceiptCases\\Sales\\{Path.GetFileNameWithoutExtension(file)}_rec.json");
                File.WriteAllText(filePath, JsonConvert.SerializeObject(fiscalDocument, Formatting.Indented));
            }
        }


        [Fact]
        public void Void()
        {
            var allFiles = Directory.GetFiles("/Users/stefankert/Desktop/Sources/GitHub/middleware/scu-it/test/fiskaltrust.Middleware.SCU.IT.CustomRTServer.UnitTest/ReceiptCases/Storno");
            foreach (var file in allFiles)
            {
                var content = File.ReadAllText(file);
                content = content.Replace("{{current_moment}}", DateTime.UtcNow.ToString("o"));
                var receiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(content);
                var (_, fiscalDocument) = CustomRTServerMapping.CreateAnnuloDocument(receiptRequest, new Models.QueueIdentification
                {
                    CashUuId = "ske00003",
                    CashHmacKey = "123djfjasdfj",
                    LastDocNumber = 1,
                    LastZNumber = 1,
                    LastSignature = "asdf",
                    CurrentGrandTotal = 1,
                    RTServerSerialNumber = "SSS"
                }, new ReceiptResponse());
                File.WriteAllText(Path.Combine($"/Users/stefankert/Desktop/Sources/GitHub/middleware/scu-it/test/fiskaltrust.Middleware.SCU.IT.CustomRTServer.UnitTest/ReceiptCases/Storno/{Path.GetFileNameWithoutExtension(file)}_rec.json"), JsonConvert.SerializeObject(fiscalDocument, Formatting.Indented));
            }
        }


        [Fact]
        public void Refund()
        {
            var allFiles = Directory.GetFiles("/Users/stefankert/Desktop/Sources/GitHub/middleware/scu-it/test/fiskaltrust.Middleware.SCU.IT.CustomRTServer.UnitTest/ReceiptCases/Refund");
            foreach (var file in allFiles)
            {
                var content = File.ReadAllText(file);
                content = content.Replace("{{current_moment}}", DateTime.UtcNow.ToString("o"));
                var receiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(content);
                var (_, fiscalDocument) = CustomRTServerMapping.CreateResoDocument(receiptRequest, new Models.QueueIdentification
                {
                    CashUuId = "ske00003",
                    CashHmacKey = "123djfjasdfj",
                    LastDocNumber = 1,
                    LastZNumber = 1,
                    LastSignature = "asdf",
                    CurrentGrandTotal = 1,
                    RTServerSerialNumber = "SSS"
                }, new ReceiptResponse());
                File.WriteAllText(Path.Combine($"/Users/stefankert/Desktop/Sources/GitHub/middleware/scu-it/test/fiskaltrust.Middleware.SCU.IT.CustomRTServer.UnitTest/ReceiptCases/Refund/{Path.GetFileNameWithoutExtension(file)}_rec.json"), JsonConvert.SerializeObject(fiscalDocument, Formatting.Indented));
            }
        }
    }
}