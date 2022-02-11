using System;
using fiskaltrust.Middleware.Contracts;
using fiskaltrust.Middleware.Queue.Helpers;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Queue.AcceptanceTest
{
    public class CryptoHelperTests
    {
        private ICryptoHelper CreateSut() => new CryptoHelper();

        [Theory]
        [InlineData(Constants.ReceiptRequest1, "3fLuQrqGKXmD452hUF/jJDo5N2k2pEpeuPxEB9oeTUg=")]
        [InlineData(Constants.ReceiptResponse1, "Pb1bTQ5wsRrtrvhh8YOaahqZiHQhi7RBnNqdo+dFpI0=")]
        public void GenerateBase64Hash_Should_Compute_CorrectHash(string data, string expectedHash)
        {
            var sut = CreateSut();
            var hash = sut.GenerateBase64Hash(data);

            hash.Should().Be(expectedHash);
        }

        [Fact]
        public void GenerateBase64ChainHash_Should_Compute_CorrectHash()
        {
            const string previousHash = "ma+D/tFGuwgKRwomnOlb6eDXjIx4JMsX3a0zqSxLpVU=";
            const string expectedHash = "u3OwD1Vtc+N22/mMF4XgIsfqPUK5HQMvYa7tfBT2sdA=";

            var receiptJournal = new ftReceiptJournal
            {
                ftReceiptJournalId = Guid.Parse("314019ae-9290-4f30-9e35-4f403df1cbaf"),
                ftReceiptNumber = 4,
                ftReceiptMoment = DateTime.Parse("2019-12-03T03:00:29.933Z").ToUniversalTime()
            };
            var queueItem = new ftQueueItem
            {
                requestHash = "3fLuQrqGKXmD452hUF/jJDo5N2k2pEpeuPxEB9oeTUg=",
                responseHash = "Pb1bTQ5wsRrtrvhh8YOaahqZiHQhi7RBnNqdo+dFpI0="
            };

            var sut = CreateSut();
            var hash = sut.GenerateBase64ChainHash(previousHash, receiptJournal, queueItem);

            hash.Should().Be(expectedHash);
        }

        [Fact]
        public void GenerateBase64ChainHash_StartReceipt_Should_Compute_CorrectHash()
        {
            const string previousHash = null;
            const string expectedHash = "s3w9N4PCwUehjsPDzziLZTArO8f3/of7c5M7NTb/99w=";

            var receiptJournal = new ftReceiptJournal
            {
                ftReceiptJournalId = Guid.Parse("314019ae-9290-4f30-9e35-4f403df1cbaf"),
                ftReceiptNumber = 4,
                ftReceiptMoment = DateTime.Parse("2019-12-03T03:00:29.933Z").ToUniversalTime()
            };


            var queueItem = new ftQueueItem
            {
                requestHash = "3fLuQrqGKXmD452hUF/jJDo5N2k2pEpeuPxEB9oeTUg=",
                responseHash = "Pb1bTQ5wsRrtrvhh8YOaahqZiHQhi7RBnNqdo+dFpI0="
            };

            var sut = CreateSut();
            var hash = sut.GenerateBase64ChainHash(previousHash, receiptJournal, queueItem);

            hash.Should().Be(expectedHash);
        }
    }
}
