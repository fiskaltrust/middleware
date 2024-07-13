using System.Linq;
using fiskaltrust.Middleware.SCU.AT.ATrustSmartcard.Services;
using FluentAssertions;
using Moq;
using Org.BouncyCastle.Asn1.X509;
using PCSC;
using PCSC.Iso7816;

namespace fiskaltrust.Middleware.SCU.AT.ATrustSmartcard.UnitTest.Services
{
    public abstract class IAtrustCardTests
    {
        protected Mock<ISCardReader> _scardReader;
        protected Mock<IIsoReader> _isoReader;
        protected CardService _sut;
        protected string _readerName = "Reader 0";
        
        [Fact]
        public void checkApplication_ShouldThrowError_IfCardReaderIsNOTSuccess()
        {

            _scardReader.Setup(x => x.BeginTransaction()).Returns(SCardError.Shutdown);

            Assert.Throws<Exception>(() => _sut.CheckApplication()).Message.Contains($"Reader {_readerName}  BeginTransaction failed");

        }
        [Fact]
        public void checkApplication_ShouldReturnTrue_IfResponceIsCorrect()
        {
            var response = new Response(new List<ResponseApdu>() { new ResponseApdu(new byte[] { 144, 0 }, IsoCase.Case4Short, SCardProtocol.Any) }, null);
            _isoReader.Setup(x => x.Transmit(It.IsAny<CommandApdu>())).Returns(response);

            _sut.CheckApplication().Should().Be(true);
        }

        [Fact]
        public void Readcertificate_ShouldThrowError_IfResponceIsNotCorrect()
        {
            var response = new Response(new List<ResponseApdu>() { new ResponseApdu(new byte[] { 0, 0 }, IsoCase.Case3Short, SCardProtocol.Any) }, null);
            _isoReader.Setup(x => x.Transmit(It.IsAny<CommandApdu>())).Returns(response);
            Assert.Throws<Exception>(() => _sut.ReadCertificates());
        }
        [Fact]
        public void Readcertificate_ShouldReturnbytes_IfResponceIsCorrect()
        {
            var data = File.ReadAllText("testdata/Certificate.txt");
            var stringbytes = data.Split(',');
            var bytes = stringbytes.Select(byte.Parse).ToArray();
            var responseData = new byte[bytes.Length + 2];
            bytes.CopyTo(responseData, 0);
            responseData[^2] = 144;
            responseData[^1] = 0;

            var response = new Response(new List<ResponseApdu>() { new ResponseApdu(responseData, IsoCase.Case3Short, SCardProtocol.Any) }, null);
            _isoReader.Setup(x => x.Transmit(It.IsAny<CommandApdu>())).Returns(response);

            _sut.ReadCertificates(true, true).Should().BeEquivalentTo(bytes);
        }

        [Fact]
        public virtual void ReadCIN_ShouldReturnnull_IfResponceIsNotCorrect()
        {
            var response = new Response(new List<ResponseApdu>() { new ResponseApdu(new byte[] { 0, 0 }, IsoCase.Case4Short, SCardProtocol.Any) }, null);
            _isoReader.Setup(x => x.Transmit(It.Is<CommandApdu>(x => x.Case == IsoCase.Case3Short))).Returns(response);

            _sut.ReadCIN().Should().BeNull();
        }

        [Fact]
        public virtual void ReadCIN_ShouldReturndata_IfResponceIsCorrect()
        {
            var response3 = new Response(new List<ResponseApdu>() { new ResponseApdu(new byte[] { 10, 144, 0 }, IsoCase.Case3Short, SCardProtocol.Any) }, null);
            var response2 = new Response(new List<ResponseApdu>() { new ResponseApdu(new byte[] { 10, 0, 0 }, IsoCase.Case2Short, SCardProtocol.Any) }, null);
            _isoReader.Setup(x => x.Transmit(It.Is<CommandApdu>(x => x.Case == IsoCase.Case3Short))).Returns(response3);
            _isoReader.Setup(x => x.Transmit(It.Is<CommandApdu>(x => x.Case == IsoCase.Case2Short))).Returns(response2);

            _sut.ReadCIN().Should().BeEquivalentTo(new byte[] { 10 });
        }
        
        [Fact]
        public void Sign_ShouldThrowError_IfResponceIsNotCorrect()
        {

            var response = new Response(new List<ResponseApdu>() { new ResponseApdu(new byte[] { 0, 0 }, IsoCase.Case4Short, SCardProtocol.Any) }, null);
            _isoReader.Setup(x => x.Transmit(It.IsAny<CommandApdu>())).Returns(response);
            Assert.Throws<Exception>(() => _sut.Sign(new byte[] { 10 }));
        }

        [Fact]
        public void Sign_ShouldReturnAnswer_IfResponceIsCorrect()
        {

            var response = new Response(new List<ResponseApdu>() { new ResponseApdu(new byte[] { 10, 144, 0 }, IsoCase.Case4Short, SCardProtocol.Any) }, null);
            _isoReader.Setup(x => x.Transmit(It.IsAny<CommandApdu>())).Returns(response);
            _sut.Sign(new byte[] { }).Should().BeEquivalentTo(new byte[] { 10 });
        }
    }
}