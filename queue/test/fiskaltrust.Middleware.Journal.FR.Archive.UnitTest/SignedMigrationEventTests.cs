using System;
using System.Linq;
using System.Text;
using fiskaltrust.Middleware.Journal.FR.Archive.V2;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Journal.FR.Archive.UnitTest
{
    /// <summary>
    /// The migration event a finish-migration receipt (0x4022) signs, read from its QR code (archive-v2 §5, market-fr#221):
    /// data-set field 20 for LNE and self-certified SCUs, the payload's <c>migration</c> object for InfoCert.
    /// </summary>
    public class SignedMigrationEventTests
    {
        private static string DataSet(string field20)
        {
            var fields = Enumerable.Repeat("", 25).ToArray();
            fields[19] = field20;
            fields[22] = "1";
            fields[23] = "N";
            return string.Join("|", fields) + ".sig";
        }

        private static string Jws(string payload)
            => "eyJhbGciOiJFUzI1NiJ9." + Convert.ToBase64String(Encoding.UTF8.GetBytes(payload)).TrimEnd('=').Replace('+', '-').Replace('/', '_') + ".sig";

        [Fact]
        public void TheDataSet_SignsOneRecordPerV1Chain()
        {
            var heads = SignedMigrationEvent.Read(DataSet("T,42,aGFzaC10,c2lnLXQ;A,3,aGFzaC1h,"));

            heads.Select(x => (x.Chain, x.Counter, x.LastHash, x.LastSignature)).Should().Equal(
                ("T", 42L, "aGFzaC10", "c2lnLXQ"), ("A", 3L, "aGFzaC1h", (string?) null));
        }

        [Fact]
        public void TheInfoCertPayload_SignsItsMigrationObject()
        {
            var heads = SignedMigrationEvent.Read(Jws("{\"chainCounter\":1,\"migration\":{\"Chains\":[{\"Chain\":\"T\",\"Counter\":42,\"LastHash\":\"aGFzaC10\",\"LastSignature\":null}]}}"));

            heads.Select(x => (x.Chain, x.Counter, x.LastHash, x.LastSignature)).Should().Equal(("T", 42L, "aGFzaC10", (string?) null));
        }

        [Theory]
        [InlineData("")]
        [InlineData("T,x,h,s")]
        [InlineData("T,1,h")]
        public void AFieldThatSignsNoEvent_DoesNotRead(string field20)
        {
            Action act = () => SignedMigrationEvent.Read(DataSet(field20));

            act.Should().Throw<FormatException>();
        }

        [Fact]
        public void APayloadWithoutMigration_DoesNotRead()
        {
            Action act = () => SignedMigrationEvent.Read(Jws("{\"chainCounter\":1}"));

            act.Should().Throw<FormatException>().WithMessage("*no migration event*");
        }
    }
}
