using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using AutoFixture;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Logs;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Helpers;
using fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Services;

namespace fiskaltrust.Middleware.SCU.DE.FiskalyCertified.IntegrationTest
{
    [Collection("FiskalySCUTests")]
    public class TestForTest
    {
        
        [Fact]
        [Trait("TseCategory", "Cloud")]
        public void Test_shouldreurnCorrect()
        {
            var apiSecret = Environment.GetEnvironmentVariable("APISECRET_FISKALYCERTIFIED_TESTS");
            var apiKey = Environment.GetEnvironmentVariable("APIKEY_FISKALYCERTIFIED_TESTS");
            var adminPin = Environment.GetEnvironmentVariable("ADMINPIN_FISKALYCERTIFIED_TESTS");
            apiSecret.Should().Be("APISECRET_FISKALYCERTIFIED_TESTS");
            apiKey.Should().Be("APIKEY_FISKALYCERTIFIED_TESTS");
            adminPin.Should().Be("ADMINPIN_FISKALYCERTIFIED_TESTS");
        }

}
}
