using System.Linq;
using System.Text;
using fiskaltrust.Middleware.SCU.AT.ATrustSmartcard.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Org.BouncyCastle.Asn1.X509;
using PCSC;
using PCSC.Iso7816;

namespace fiskaltrust.Middleware.SCU.AT.ATrustSmartcard.UnitTest.Services
{
    public class CardServiceFactoryTests
    {
        private readonly Mock<ISCardReader> _scardReader;
        private readonly Mock<IIsoReader> _isoReader;
        private readonly SCardReaderState _sCardReaderState;
        private readonly CardServiceFactory _sut;
        private readonly string _readerName = "Reader 0";
        public CardServiceFactoryTests()
        {
            _scardReader = new Mock<ISCardReader>();
            _isoReader = new Mock<IIsoReader>();
            _sCardReaderState = new SCardReaderState();

            _isoReader.Setup(x => x.ReaderName).Returns(_readerName);
            _scardReader.Setup(x => x.CurrentContext.GetReaderStatus(_readerName)).Returns(_sCardReaderState);

            _sut = new CardServiceFactory(Mock.Of<ILogger<CardServiceFactory>>());

        }
        ~CardServiceFactoryTests()
        {
            _sCardReaderState.Dispose();
        }

        [Theory()]
        [InlineData("3B-BF-11-00-81-31-FE-45-45-50-41")]
        [InlineData("3B-BF-11-00-81-31-FE-45-4D-43-41")]
        public void CreateCardService_ShouldReturnatrustACOSObject(string atr)
        {
            _sCardReaderState.Atr = atr.Split('-').Select(b => Convert.ToByte(b, 16)).ToArray();

            _sut.CreateCardService(_scardReader.Object, _isoReader.Object).Should().BeOfType<AtrustACOS>();
        }

        [Theory()]
        [InlineData("3B-DF-18-00-81-31-FE-58-80-31-B0-52-02-04-64-05-C9-03-AC-73-B7-B1-D4-22")]
        [InlineData("3B-DF-18-00-81-31-FE-58-80-31-90")]
        public void CreateCardService_ShouldReturnatrustCardOS53Object(string atr)
        {
            _sCardReaderState.Atr = atr.Split('-').Select(b => Convert.ToByte(b, 16)).ToArray();

            _sut.CreateCardService(_scardReader.Object, _isoReader.Object).Should().BeOfType<AtrustCardOS53>();
        }

        [Theory()]
        [InlineData("3B-DF-96-FF-91-01-31-FE-46-80-31-90-52-41-02-64-05-02-00-AC-73-D6-22-C0-17")]
        [InlineData("3B-DF-18-FF-91-01-31-FE-46-80-31-90-52-41-02-64-05-02-00-AC-73-D6-22-C0-99")]
        [InlineData("3B-DF-97-00-81-31-FE-46-80-31-90-52-41-03-64-05-02-01-AC-73-D6-22-C0-F8")]
        [InlineData("3B-DF-97-00-81-31-FE-46-80-31-90-52-41-02-64-05-C9-03-AC-73-D6-22-C0-30")]
        [InlineData("3B-DF-96-FF-91-81-31-FE-46-80-31-90-52-41-02-64-05-C9-03-AC-73-D6-22-C0-5F")]
        [InlineData("3B-DF-18-FF-91-81-31-FE-46-80-31-90-52-41-02-64-05-C9-03-AC-73-D6-22-C0-D1")]
        public void CreateCardService_ShouldReturnatrustAcosIDObject(string atr)
        {
            _sCardReaderState.Atr = atr.Split('-').Select(b => Convert.ToByte(b, 16)).ToArray();

            _sut.CreateCardService(_scardReader.Object, _isoReader.Object).Should().BeOfType<AtrustAcosID>();
        }
    }
}