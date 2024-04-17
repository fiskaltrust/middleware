using System.Linq;
using fiskaltrust.Middleware.SCU.AT.ATrustSmartcard.Services;
using FluentAssertions;
using Moq;
using Org.BouncyCastle.Asn1.X509;
using PCSC;
using PCSC.Iso7816;

namespace fiskaltrust.Middleware.SCU.AT.ATrustSmartcard.UnitTest.Services
{
    public class AtrustACOSCardTests: IAtrustCardTests
    {
        public AtrustACOSCardTests()
        {
            _scardReader = new Mock<ISCardReader>();
            _isoReader = new Mock<IIsoReader>();

            _isoReader.Setup(x => x.ReaderName).Returns(_readerName);
            _scardReader.Setup(x => x.BeginTransaction()).Returns(SCardError.Success);
            _sut = new AtrustACOS(_scardReader.Object, _isoReader.Object);

        }
      
        [Fact]
        public override void ReadCIN_ShouldReturnnull_IfResponceIsNotCorrect()
        {
            var response = new Response(new List<ResponseApdu>() { new ResponseApdu(new byte[] { 0, 0 }, IsoCase.Case4Short, SCardProtocol.Any) }, null);
            _isoReader.Setup(x => x.Transmit(It.Is<CommandApdu>(x => x.Case == IsoCase.Case4Short))).Returns(response);

            _sut.ReadCIN().Should().BeNull();
        }

        [Fact]
        public override void ReadCIN_ShouldReturndata_IfResponceIsCorrect()
        {
            var response4 = new Response(new List<ResponseApdu>() { new ResponseApdu(new byte[] { 10, 144, 0 }, IsoCase.Case4Short, SCardProtocol.Any) }, null);
            var response2 = new Response(new List<ResponseApdu>() { new ResponseApdu(new byte[] { 10, 0, 0 }, IsoCase.Case2Short, SCardProtocol.Any) }, null);
            _isoReader.Setup(x => x.Transmit(It.Is<CommandApdu>(x => x.Case == IsoCase.Case4Short))).Returns(response4);
            _isoReader.Setup(x => x.Transmit(It.Is<CommandApdu>(x => x.Case == IsoCase.Case2Short))).Returns(response2);

            _sut.ReadCIN().Should().BeEquivalentTo(new byte[] { 10 });
        }

        [Fact]
        public void SelectApplication_ShouldBeFalse_IfResponceIsNotCorrect()
        {

            var response = new Response(new List<ResponseApdu>() { new ResponseApdu(new byte[] { 0, 0 }, IsoCase.Case4Short, SCardProtocol.Any) }, null);
            _isoReader.Setup(x => x.Transmit(It.IsAny<CommandApdu>())).Returns(response);
            _sut.SelectApplication().Should().BeFalse();
        }

        [Fact]
        public void SelectApplication_ShouldBeFalse_IfResponceIsCorrect()
        {

            var response = new Response(new List<ResponseApdu>() { new ResponseApdu(new byte[] { 144, 0 }, IsoCase.Case4Short, SCardProtocol.Any) }, null);
            _isoReader.Setup(x => x.Transmit(It.IsAny<CommandApdu>())).Returns(response);
            _sut.SelectApplication().Should().BeTrue();
        }
      
    }
}