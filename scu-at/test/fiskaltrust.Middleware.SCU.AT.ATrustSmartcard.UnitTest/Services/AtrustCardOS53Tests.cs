using System.Linq;
using fiskaltrust.Middleware.SCU.AT.ATrustSmartcard.Services;
using FluentAssertions;
using Moq;
using Org.BouncyCastle.Asn1.X509;
using PCSC;
using PCSC.Iso7816;

namespace fiskaltrust.Middleware.SCU.AT.ATrustSmartcard.UnitTest.Services
{
    public class AtrustCardOS53Tests : IAtrustCardTests
    {
        public AtrustCardOS53Tests()
        {
            _scardReader = new Mock<ISCardReader>();
            _isoReader = new Mock<IIsoReader>();

            _isoReader.Setup(x => x.ReaderName).Returns(_readerName);
            _scardReader.Setup(x => x.BeginTransaction()).Returns(SCardError.Success);
            _sut = new AtrustCardOS53(_scardReader.Object, _isoReader.Object);

        }

        [Fact]
        public void SelectApplication_ShouldBeTrue()
        {
            _sut.SelectApplication().Should().BeTrue();
        }
    }
}