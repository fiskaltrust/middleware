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
    }
}