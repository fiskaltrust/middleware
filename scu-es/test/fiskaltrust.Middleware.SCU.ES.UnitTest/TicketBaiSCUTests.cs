using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.ES.TicketBAI;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.ES.UnitTest
{
    public class TicketBaiSCUTests
    {
        [Fact]
        public async Task SubmitInvoiceAsync()
        {
            var cert = new X509Certificate2(@"TestCertificates/dispositivo_act.p12", "Iz3np32023", X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
            var config = new TicketBaiSCUConfiguration
            {
                Certificate = cert
            };            
            var sut = new TicketBaiSCU(NullLogger<TicketBaiSCU>.Instance, config);
            var response = await sut.SubmitInvoiceAsync(TicketBaiDemo.GetTicketBayRequest());
            response.Should().NotBeNull();
        }
    }
}
