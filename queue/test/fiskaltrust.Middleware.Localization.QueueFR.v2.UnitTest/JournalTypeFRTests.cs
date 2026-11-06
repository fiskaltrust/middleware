using fiskaltrust.Middleware.Storage.Base.Interface;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;
using fiskaltrust.Middleware.Localization.v2.Interface;
using FluentAssertions;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.UnitTest;

public class JournalTypeFRTests
{
    // The codes of the v2 archive contract (market-fr doc/nf525/contracts/archive-v2.md, section 7).
    [Theory]
    [InlineData(0x000, JournalTypeFR.StatusInformation)]
    [InlineData(0x001, JournalTypeFR.Ticket)]
    [InlineData(0x002, JournalTypeFR.PaymentProof)]
    [InlineData(0x003, JournalTypeFR.Invoice)]
    [InlineData(0x004, JournalTypeFR.Technical)]
    [InlineData(0x007, JournalTypeFR.Bill)]
    [InlineData(0x009, JournalTypeFR.Log)]
    [InlineData(0x00A, JournalTypeFR.Copy)]
    [InlineData(0x00B, JournalTypeFR.Training)]
    [InlineData(0x00C, JournalTypeFR.ShiftClosing)]
    [InlineData(0x00D, JournalTypeFR.DailyClosing)]
    [InlineData(0x00E, JournalTypeFR.MonthlyClosing)]
    [InlineData(0x00F, JournalTypeFR.YearlyClosing)]
    [InlineData(0x010, JournalTypeFR.ArchiveExport)]
    public void JournalTypeFR_FollowsTheArchiveContractCodes(long code, JournalTypeFR expected)
    {
        ((long) expected).Should().Be(0x4652_2000_0000_0000 | code);
    }

    [Fact]
    public void JournalTypeFR_ListsFourteenJournals()
    {
        Enum.GetValues<JournalTypeFR>().Should().HaveCount(14).And.OnlyHaveUniqueItems();
    }

    [Theory]
    [InlineData(0x4652_2000_0000_0007)] // v2-shaped request
    [InlineData(0x4652_0000_0000_0007)] // v1-shaped request (no version nibble)
    [InlineData(0x4652_2000_0001_0007)] // with a gggg flag set
    public void TypeFR_MatchesOnTheJournalCodeOnly(long ftJournalType)
    {
        var journalType = (JournalType) ftJournalType;

        journalType.TypeFR().Should().Be(JournalTypeFR.Bill);
        journalType.IsType(JournalTypeFR.Bill).Should().BeTrue();
        journalType.IsType(JournalTypeFR.Log).Should().BeFalse();
    }

    [Fact]
    public void JournalProcessorFR_RefusesTheRetiredArchiveGroup008()
    {
        var act = () => Sut().ProcessAsync(new JournalRequest { ftJournalType = (JournalType) 0x4652_2000_0000_0008 });

        act.Should().Throw<ArgumentException>().WithMessage("*is unknown*");
    }

    [Fact]
    public void JournalProcessorFR_RefusesTheArchiveExportWithoutAnArchiveSigner()
    {
        var act = () => Sut().ProcessAsync(new JournalRequest { ftJournalType = JournalTypeFR.ArchiveExport.As<JournalType>() });

        act.Should().Throw<NotSupportedException>().WithMessage("*needs an archive signer*");
    }

    [Fact]
    public async Task ScuArchiveSigner_RefusesAnScuThatDoesNotSignArchives()
    {
        // FakeFRSSCD keeps the contract's default for GetSigningCertificateAsync, as an SCU without archive signing does.
        var act = () => ScuArchiveSigner.CreateAsync(new FakeFRSSCD());

        await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*SCU cannot sign the archive export*does not sign archives*");
    }

    [Fact]
    public async Task ScuArchiveSigner_RefusesAHashTheScuReportsItDidNotSign()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var certificate = new CertificateRequest("CN=SCU FR", key, HashAlgorithmName.SHA256).CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        var scu = new Mock<IFRSSCD>();
        scu.Setup(x => x.GetSigningCertificateAsync()).ReturnsAsync(new FRSigningCertificate { CertificateBase64 = Convert.ToBase64String(certificate.RawData), CertificateSerialNumber = "CERT-4711" });
        scu.Setup(x => x.SignHashAsync(It.IsAny<byte[]>())).ReturnsAsync(FRHashSignature.Failed("the key is unavailable"));
        var sut = await ScuArchiveSigner.CreateAsync(scu.Object);

        var act = () => sut.SignHashAsync(new byte[32], CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*SCU cannot sign the archive export*the key is unavailable*");
    }

    [Theory]
    [InlineData(JournalTypeFR.Ticket, "text/csv")]
    [InlineData(JournalTypeFR.Technical, "text/csv")]
    [InlineData(JournalTypeFR.DailyClosing, "text/csv")]
    [InlineData(JournalTypeFR.Training, "text/csv")]
    [InlineData(JournalTypeFR.StatusInformation, "application/json")]
    public void JournalProcessorFR_AnswersTheGroupJournalsAndTheStatus(JournalTypeFR journalTypeFR, string mediaType)
    {
        var (contentType, _) = Sut().ProcessAsync(new JournalRequest { ftJournalType = journalTypeFR.As<JournalType>() });

        contentType.MediaType.Should().Be(mediaType);
    }

    [Fact]
    public void JournalProcessorFR_RejectsANegativeTo()
    {
        var act = () => Sut().ProcessAsync(new JournalRequest { ftJournalType = JournalTypeFR.Ticket.As<JournalType>(), To = -10 });

        act.Should().Throw<ArgumentException>().WithMessage("*negative To*");
    }

    private static JournalProcessorFR Sut() => new(Guid.NewGuid(), Mock.Of<IStorageProvider>());

    [Theory]
    [InlineData(0x4652_2000_0000_0005)]
    [InlineData(0x4652_2000_0000_0006)]
    [InlineData(0x4652_2000_0000_0008)] // the retired v1 archive group
    [InlineData(0x4652_2000_0000_0011)]
    public void JournalProcessorFR_RejectsUnknownFrenchJournalCodes(long ftJournalType)
    {
        var sut = Sut();

        var act = () => sut.ProcessAsync(new JournalRequest { ftJournalType = (JournalType) ftJournalType });

        act.Should().Throw<ArgumentException>().WithMessage("*unknown*");
    }

    [Fact]
    public void JournalProcessorFR_RejectsAnotherCountrysJournalType()
    {
        var sut = Sut();

        var act = () => sut.ProcessAsync(new JournalRequest { ftJournalType = (JournalType) 0x4154_0000_0000_0001 });

        act.Should().Throw<ArgumentException>().WithMessage("*not a French journal type*");
    }
}
