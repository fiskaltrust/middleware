using System;
using fiskaltrust.Middleware.Contracts.Interfaces;
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

        [Fact]
        public void GenerateBase64ChainHash_ShouldReturnExpectedHash_WhenValidInput()
        {
            var queueItem0 = new ftQueueItem
            {
                requestHash = "1mky6HKWz6pbkHwE97if2gWTXyitQ5Yg/uYv8tvemDA=",
                responseHash = "Nc9+/uql5WPTWnhSjtse950DxWy4z+mK6LRrcCujnC0=",
            };

            var receiptJournal0 = new ftReceiptJournal
            {
                ftReceiptJournalId = Guid.Parse("12ae1ae5-f5cc-409a-9eab-8ca92c3ad954"),
                ftReceiptMoment = DateTime.Parse("2024-06-27T14:53:33.9663641Z").AddHours(-4),
                ftReceiptNumber = 1,
                ftReceiptHash = "HW6y1kl1c30glF/wgySf5P0pigaTA9KnUAbHGC/pEZ4="
            };

            var sut = CreateSut();
            var requestHash0 = sut.GenerateBase64Hash("""{"ftCashBoxID":"a1b8efc9-7678-4ce0-9f33-48df1e70fb8c","ftPosSystemId":"2989d51c-e7e5-ea11-a817-000d3a49ee32","cbTerminalID":"w2k16-protel","cbReceiptReference":"TSE-2024-000001","cbReceiptMoment":"2024-06-27T12:53:30+00:00","cbChargeItems":[],"cbPayItems":[],"ftReceiptCase":4919338172267102211,"ftReceiptCaseData":"{\"UserId\":\"BERGEPROHOTELEDV\"}","cbUser":"berge@prohotel-edv.de#"}""");
            var responseHash0 = sut.GenerateBase64Hash("""{"ftCashBoxID":"a1b8efc9-7678-4ce0-9f33-48df1e70fb8c","ftQueueID":"56f88925-7458-4109-aae3-cbcc3641586f","ftQueueItemID":"711e7fb1-d44d-492a-b739-0357bad7d051","ftQueueRow":1,"cbTerminalID":"w2k16-protel","cbReceiptReference":"TSE-2024-000001","ftCashBoxIdentification":"JYn4Vlh0CUGq48vMNkFYbw","ftReceiptIdentification":"ft0#IT1","ftReceiptMoment":"2024-06-27T12:53:31.1217091Z","ftSignatures":[{"ftSignatureFormat":65549,"ftSignatureType":4919338167972134928,"Caption":"start-transaction-result","Data":"umlYWr6GrbizJEWm8RjY0HHaEvVIwsnN7XnrKfYiQsQkFGVYo/KpiKl1LQpQ6Wvwl81KhyJoDZ11h94tfs/EGA=="},{"ftSignatureFormat":65549,"ftSignatureType":4919338167972134929,"Caption":"finish-transaction-payload","Data":"aW5pdGlhbCBvcGVyYXRpb24gcmVjZWlwdCAvIHN0YXJ0LXJlY2VpcHQ="},{"ftSignatureFormat":65549,"ftSignatureType":4919338167972134930,"Caption":"finish-transaction-result","Data":"M45QdVUZ3wgUxQXQv72VVZT1lp/Q2SLw6ND3ebMW/BDwdzCe03nnGMjb076bCxWZDF4u/VQt6t37avbmBYmSGw=="},{"ftSignatureFormat":65537,"ftSignatureType":4919338167972134933,"Caption":"<processType>","Data":"SonstigerVorgang"},{"ftSignatureFormat":65537,"ftSignatureType":4919338167972134932,"Caption":"<kassen-seriennummer>","Data":"JYn4Vlh0CUGq48vMNkFYbw"},{"ftSignatureFormat":65537,"ftSignatureType":4919338167972134934,"Caption":"<processData>","Data":"initial operation receipt / start-receipt"},{"ftSignatureFormat":65537,"ftSignatureType":4919338167972134935,"Caption":"<transaktions-nummer>","Data":"1"},{"ftSignatureFormat":65537,"ftSignatureType":4919338167972134936,"Caption":"<signatur-zaehler>","Data":"192"},{"ftSignatureFormat":65537,"ftSignatureType":4919338167972134937,"Caption":"<start-zeit>","Data":"2024-06-27T12:53:32.000Z"},{"ftSignatureFormat":65537,"ftSignatureType":4919338167972134938,"Caption":"<log-time>","Data":"2024-06-27T12:53:32.000Z"},{"ftSignatureFormat":65537,"ftSignatureType":4919338167972134941,"Caption":"<signatur>","Data":"M45QdVUZ3wgUxQXQv72VVZT1lp/Q2SLw6ND3ebMW/BDwdzCe03nnGMjb076bCxWZDF4u/VQt6t37avbmBYmSGw=="},{"ftSignatureFormat":65537,"ftSignatureType":4919338167972134939,"Caption":"<sig-alg>","Data":"ecdsa-plain-SHA256"},{"ftSignatureFormat":65537,"ftSignatureType":4919338167972134940,"Caption":"<log-time-format>","Data":"unixTime"},{"ftSignatureFormat":1,"ftSignatureType":4919338167972134946,"Caption":"<certification-id>","Data":"BSI-K-TR-0490-2021"},{"ftSignatureFormat":65537,"ftSignatureType":4919338167972134942,"Caption":"<public-key>","Data":"BCDbvZw4kY3/4DQDJPbqFgVEc/3Z9wwHWAk+dIMTbEwE/5N3Tj6sUhvMxXWD4z7PctM+AqHuAmAx4fxjYzdg7cA="},{"ftSignatureFormat":1,"ftSignatureType":4919338167972134914,"Caption":"In-Betriebnahme-Beleg","Data":"Kassenseriennummer: JYn4Vlh0CUGq48vMNkFYbw, TSE-Seriennummer: 43a3e2bb20d2d29cb8e379281ef0a49a2f239c871d88e80f19dde071f2023d0c, Queue-ID: 56f88925-7458-4109-aae3-cbcc3641586f"},{"ftSignatureFormat":8,"ftSignatureType":4919338167972134915,"Caption":"In-Betriebnahme-Beleg. Kassenseriennummer: JYn4Vlh0CUGq48vMNkFYbw, TSE-Seriennummer: 43a3e2bb20d2d29cb8e379281ef0a49a2f239c871d88e80f19dde071f2023d0c, Queue-ID: 56f88925-7458-4109-aae3-cbcc3641586f","Data":"{\"CashBoxId\":\"a1b8efc9-7678-4ce0-9f33-48df1e70fb8c\",\"QueueId\":\"56f88925-7458-4109-aae3-cbcc3641586f\",\"Moment\":\"2024-06-27T12:53:33.8383276Z\",\"CashBoxIdentification\":\"JYn4Vlh0CUGq48vMNkFYbw\",\"SCUId\":\"960ce033-90e5-48c9-ac9b-17b7f4c3d3de\",\"SCUPackageName\":null,\"SCUSignatureAlgorithm\":\"ecdsa-plain-SHA256\",\"SCUPublicKeyBase64\":\"BCDbvZw4kY3/4DQDJPbqFgVEc/3Z9wwHWAk+dIMTbEwE/5N3Tj6sUhvMxXWD4z7PctM+AqHuAmAx4fxjYzdg7cA=\",\"SCUSerialNumberBase64\":\"43a3e2bb20d2d29cb8e379281ef0a49a2f239c871d88e80f19dde071f2023d0c\",\"IsStartReceipt\":true,\"Version\":\"V0\"}"}],"ftState":4919338167972134912,"ftStateData":"{\"TseInfo\":{\"MaxNumberOfClients\":199,\"CurrentNumberOfClients\":1,\"CurrentClientIds\":[\"JYn4Vlh0CUGq48vMNkFYbw\"],\"MaxNumberOfStartedTransactions\":2000,\"CurrentNumberOfStartedTransactions\":0,\"CurrentStartedTransactionNumbers\":[],\"MaxNumberOfSignatures\":9223372036854775807,\"CurrentNumberOfSignatures\":192,\"MaxLogMemorySize\":9223372036854775807,\"CurrentLogMemorySize\":-1,\"CurrentState\":1,\"FirmwareIdentification\":\"2.1.16\",\"CertificationIdentification\":\"BSI-K-TR-0490-2021\",\"SignatureAlgorithm\":\"ecdsa-plain-SHA256\",\"LogTimeFormat\":\"unixTime\",\"SerialNumberOctet\":\"43a3e2bb20d2d29cb8e379281ef0a49a2f239c871d88e80f19dde071f2023d0c\",\"PublicKeyBase64\":\"BCDbvZw4kY3/4DQDJPbqFgVEc/3Z9wwHWAk+dIMTbEwE/5N3Tj6sUhvMxXWD4z7PctM+AqHuAmAx4fxjYzdg7cA=\",\"CertificatesBase64\":[\"MIIEczCCA/qgAwIBAgIQF66T60B76FY6oJsIYB33IzAKBggqhkjOPQQDAzBFMRswGQYDVQQDExJEQVJaLVRTRS1TVUItQ0EtMDExDTALBgNVBAoTBERBUloxCzAJBgNVBAYTAkRFMQowCAYDVQQFEwEyMB4XDTI0MDExMDE2NDcyNloXDTMyMDExMDIzNTk1OVowgdQxSTBHBgNVBAMMQEJTSS1EU1otQ0MtMTE1M19CU0ktRFNaLUNDLTExMzBfNzRFQThFOTgyRUQ2NENEQThDNjJCRDdDMkQyMUU3MjgxFTATBgNVBAoTDGZpc2thbHkgR21iSDEYMBYGA1UELhMPQlNJLURTWi1DQy0xMTUzMUkwRwYDVQQFE0A0M2EzZTJiYjIwZDJkMjljYjhlMzc5MjgxZWYwYTQ5YTJmMjM5Yzg3MWQ4OGU4MGYxOWRkZTA3MWYyMDIzZDBjMQswCQYDVQQGEwJERTBZMBMGByqGSM49AgEGCCqGSM49AwEHA0IABCDbvZw4kY3/4DQDJPbqFgVEc/3Z9wwHWAk+dIMTbEwE/5N3Tj6sUhvMxXWD4z7PctM+AqHuAmAx4fxjYzdg7cCjggI6MIICNjAfBgNVHSMEGDAWgBRsySR+ZuH5TCT2MIwLaxq4tIczejAOBgNVHQ8BAf8EBAMCB4AwDAYDVR0TAQH/BAIwADBaBgNVHSAEUzBRME8GCisGAQQBg5NvAQMwQTA/BggrBgEFBQcCARYzaHR0cHM6Ly93d3cuZGEtcnouZGUvZGUvdWViZXItZGFyei91bnRlcm5laG1lbi9wa2kvMFQGA1UdEgRNMEuBFHRzZS1yb290LWNhQGRhLXJ6LmRlhjNodHRwczovL3d3dy5kYS1yei5kZS9kZS91ZWJlci1kYXJ6L3VudGVybmVobWVuL3BraS8wgfEGA1UdHwSB6TCB5jByoHCgboZsaHR0cDovL3RzZS1wa2kuZGEtcnoubmV0L3RzZS1wa2kvY3JsP2lzc3VlckROPUNOJTNEREFSWi1UU0UtU1VCLUNBLTAxJTJDTyUzRERBUlolMkNDJTNEREUlMkNTRVJJQUxOVU1CRVIlM0QyMHCgbqBshmpsZGFwOi8vbGRhcC10c2UtcGtpLmRhLXJ6Lm5ldC9zZXJpYWxOdW1iZXI9MixDTj1EQVJaLVRTRS1TVUItQ0EtMDEsREM9REFSWixEQz1ERT9jZXJ0aWZpY2F0ZVJldm9jYXRpb25MaXN0ME8GCCsGAQUFBwEBBEMwQTA/BggrBgEFBQcwAoYzaHR0cHM6Ly93d3cuZGEtcnouZGUvZGUvdWViZXItZGFyei91bnRlcm5laG1lbi9wa2kvMAoGCCqGSM49BAMDA2cAMGQCMHXaPVBsD3UWBRlw2ermGZNLP3WPzjebfUIalRkIb4Py4WtRPuJPq2GieWvVulSwLgIwbQuFrbU5FwZy6KC4TKgHnDodfs9P4/bSA0H1yGYAYT5r2myZWpY9DnoWpUPgN6tc\"],\"Info\":null}}"}""");

            requestHash0.Should().Be(queueItem0.requestHash);
            responseHash0.Should().Be(queueItem0.responseHash);

            var receiptHash0 = sut.GenerateBase64ChainHash("", receiptJournal0, queueItem0);

            receiptHash0.Should().Be(receiptJournal0.ftReceiptHash);

            var queueItem1 = new ftQueueItem
            {
                requestHash = "tim9T6meGV1r+RXRjmxLUGib5zR7Qwn1MTFfllcN/UA=",
                responseHash = "Rtaqqz2006vSmIqXVV0M0b2Kogdz6iq9H5dos5B98S4=",
            };

            var receiptJournal1 = new ftReceiptJournal
            {
                ftReceiptJournalId = Guid.Parse("52502cf3-7ade-415f-8691-2117c902eb2b"),
                ftReceiptMoment = DateTime.Parse("2024-07-29T10:05:34.8939324Z").AddHours(-2),
                ftReceiptNumber = 2,
                ftReceiptHash = "1nMGoQuwo5ezHyodgu2ZRk94a60hNvfEdEbdanq7+UE="
            };

            var requestHash1 = sut.GenerateBase64Hash("""{"ftCashBoxID":"a1b8efc9-7678-4ce0-9f33-48df1e70fb8c","ftPosSystemId":"2989d51c-e7e5-ea11-a817-000d3a49ee32","cbTerminalID":"<web>","cbReceiptReference":"TSE-2024-000002","cbReceiptMoment":"2024-07-29T10:05:28+00:00","cbChargeItems":[],"cbPayItems":[],"ftReceiptCase":4919338172267102215,"ftReceiptCaseData":"{\"UserId\":\"TOERSELPROHOTELE\"}","cbUser":"toersel@prohotel-edv.de#"}""");
            var responseHash1 = sut.GenerateBase64Hash("""{"ftCashBoxID":"a1b8efc9-7678-4ce0-9f33-48df1e70fb8c","ftQueueID":"56f88925-7458-4109-aae3-cbcc3641586f","ftQueueItemID":"e48d72ea-d6c1-4ea9-a086-12f8e43778dc","ftQueueRow":2,"cbTerminalID":"<web>","cbReceiptReference":"TSE-2024-000002","ftCashBoxIdentification":"JYn4Vlh0CUGq48vMNkFYbw","ftReceiptIdentification":"ft1#IT2","ftReceiptMoment":"2024-07-29T10:05:31.7418252Z","ftSignatures":[{"ftSignatureFormat":65549,"ftSignatureType":4919338167972134928,"Caption":"start-transaction-result","Data":"w/b7Tl4GsNGiUdGE/wfVNlzQpXJ1bV7KS5m/OCNrJyrLwTnZTLnbr5X2yFZ8JHMjSwHl94L5qpUfbWXkxEwldw=="},{"ftSignatureFormat":65549,"ftSignatureType":4919338167972134929,"Caption":"finish-transaction-payload","Data":"ZGFpbHktY2xvc2luZw=="},{"ftSignatureFormat":65549,"ftSignatureType":4919338167972134930,"Caption":"finish-transaction-result","Data":"FRXTx5ucs8GWAC4zP/cenWlD2VPo/EERlFw12KQ+zND1wCh79Yjm/5AO73ocI7/nJu+pKs1cDO+5tUQ0uc8MHw=="},{"ftSignatureFormat":65537,"ftSignatureType":4919338167972134933,"Caption":"<processType>","Data":"SonstigerVorgang"},{"ftSignatureFormat":65537,"ftSignatureType":4919338167972134932,"Caption":"<kassen-seriennummer>","Data":"JYn4Vlh0CUGq48vMNkFYbw"},{"ftSignatureFormat":65537,"ftSignatureType":4919338167972134934,"Caption":"<processData>","Data":"daily-closing"},{"ftSignatureFormat":65537,"ftSignatureType":4919338167972134935,"Caption":"<transaktions-nummer>","Data":"2"},{"ftSignatureFormat":65537,"ftSignatureType":4919338167972134936,"Caption":"<signatur-zaehler>","Data":"226"},{"ftSignatureFormat":65537,"ftSignatureType":4919338167972134937,"Caption":"<start-zeit>","Data":"2024-07-29T10:05:31.000Z"},{"ftSignatureFormat":65537,"ftSignatureType":4919338167972134938,"Caption":"<log-time>","Data":"2024-07-29T10:05:31.000Z"},{"ftSignatureFormat":65537,"ftSignatureType":4919338167972134941,"Caption":"<signatur>","Data":"FRXTx5ucs8GWAC4zP/cenWlD2VPo/EERlFw12KQ+zND1wCh79Yjm/5AO73ocI7/nJu+pKs1cDO+5tUQ0uc8MHw=="},{"ftSignatureFormat":65537,"ftSignatureType":4919338167972134939,"Caption":"<sig-alg>","Data":"ecdsa-plain-SHA256"},{"ftSignatureFormat":65537,"ftSignatureType":4919338167972134940,"Caption":"<log-time-format>","Data":"unixTime"},{"ftSignatureFormat":1,"ftSignatureType":4919338167972134946,"Caption":"<certification-id>","Data":"BSI-K-TR-0490-2021"},{"ftSignatureFormat":65537,"ftSignatureType":4919338167972134942,"Caption":"<public-key>","Data":"BCDbvZw4kY3/4DQDJPbqFgVEc/3Z9wwHWAk+dIMTbEwE/5N3Tj6sUhvMxXWD4z7PctM+AqHuAmAx4fxjYzdg7cA="}],"ftState":4919338167972134912,"ftStateData":"{\"MasterData\":{\"Account\":{\"AccountId\":\"21c4bf4f-f56e-414f-b90b-6156a4a94a38\",\"AccountName\":\"Erlebnishotel \\\"Zur Schiffsmühle\\\" GmbH              \",\"Street\":\"Zur Schiffsmühle 2\",\"Zip\":\"04668\",\"City\":\"Grimma\",\"Country\":\"DE\",\"TaxId\":null,\"VatId\":\"DE326822181\"},\"Outlet\":{\"OutletId\":\"b2c5714d-11c1-4b9d-8b25-8f9a4777d1f6\",\"OutletName\":\"Erlebnishotel \\\"Zur Schiffsmühle\\\" GmbH ASA\",\"Street\":\"Zur Schiffsmühle 2\",\"Zip\":\"04668\",\"City\":\"Grimma\",\"Country\":\"DE\",\"VatId\":\"DE326822181\",\"LocationId\":null},\"Agencies\":[],\"PosSystems\":[{\"PosSystemId\":\"18a8d87e-89a5-ea11-a812-000d3a49ee32\",\"Brand\":\"protel hotelsoftware\",\"Model\":\"Cloud\",\"SoftwareVersion\":null,\"BaseCurrency\":\"EUR\",\"Type\":null},{\"PosSystemId\":\"2989d51c-e7e5-ea11-a817-000d3a49ee32\",\"Brand\":\"ASA Hotel\",\"Model\":\"Hotelsoftware\",\"SoftwareVersion\":null,\"BaseCurrency\":\"EUR\",\"Type\":null},{\"PosSystemId\":\"688dbf72-89a5-ea11-a812-000d3a49ee32\",\"Brand\":\"protel hotelsoftware\",\"Model\":\"On Premise\",\"SoftwareVersion\":null,\"BaseCurrency\":\"EUR\",\"Type\":null}]},\"DailyClosingNumber\":1}"}""");


            requestHash1.Should().Be(queueItem1.requestHash);
            responseHash1.Should().Be(queueItem1.responseHash);

            var receiptHash1 = sut.GenerateBase64ChainHash(receiptJournal0.ftReceiptHash, receiptJournal1, queueItem1);

            receiptHash1.Should().Be(receiptJournal1.ftReceiptHash);


            var queueItem2 = new ftQueueItem
            {
                requestHash = "bdhKqjAoDPG0fU+sou25p8wwhDVHQ7duvQygw0vF7pw=",
                responseHash = "ICMUJGhhpQjqv5KeOmt+JRGnhG9qXj99SwfLcXfAYlE=",
            };

            var receiptJournal2 = new ftReceiptJournal
            {
                ftReceiptJournalId = Guid.Parse("6026a223-1a81-4892-b582-f7cd4facf193"),
                ftReceiptMoment = DateTime.Parse("2024-07-29T10:05:37.4252624Z").AddHours(-2),
                ftReceiptNumber = 3,
                ftReceiptHash = "r99BZ7RcWUHMzkIDhmUOWDjntC5S/4ckddFOWkzhM5A="
            };

            var requestHash2 = sut.GenerateBase64Hash("""{"ftCashBoxID":"a1b8efc9-7678-4ce0-9f33-48df1e70fb8c","ftPosSystemId":"2989d51c-e7e5-ea11-a817-000d3a49ee32","cbTerminalID":"<web>","cbReceiptReference":"TSE-2024-000003","cbReceiptMoment":"2024-07-29T10:05:35+00:00","cbChargeItems":[],"cbPayItems":[],"ftReceiptCase":4919338172267102213,"ftReceiptCaseData":"{\"UserId\":\"TOERSELPROHOTELE\"}","cbUser":"toersel@prohotel-edv.de#"}""");
            var responseHash2 = sut.GenerateBase64Hash("""{"ftCashBoxID":"a1b8efc9-7678-4ce0-9f33-48df1e70fb8c","ftQueueID":"56f88925-7458-4109-aae3-cbcc3641586f","ftQueueItemID":"f6808b6a-1b5f-4e41-99c1-1645f832198e","ftQueueRow":3,"cbTerminalID":"<web>","cbReceiptReference":"TSE-2024-000003","ftCashBoxIdentification":"JYn4Vlh0CUGq48vMNkFYbw","ftReceiptIdentification":"ft2#IT3","ftReceiptMoment":"2024-07-29T10:05:35.2287826Z","ftSignatures":[{"ftSignatureFormat":65549,"ftSignatureType":4919338167972134928,"Caption":"start-transaction-result","Data":"08+3zWs1x7bRgsM4/AtN7KWq1tdUh3navTj00hTJdHyHUH75VOHTiEwAUn1PaBSv8sEoXwkovd4B+Ih7A7VIQw=="},{"ftSignatureFormat":65549,"ftSignatureType":4919338167972134929,"Caption":"finish-transaction-payload","Data":"bW9udGhseS1jbG9zaW5n"},{"ftSignatureFormat":65549,"ftSignatureType":4919338167972134930,"Caption":"finish-transaction-result","Data":"IhEUPZQwSJ3D2ZpUM9YXxJ4eApXInrCRqBuJhsBKq48uFyiHyQ7vbTD9KdnDI7KOugLhPHQbVBAHBrR+cxkfXw=="},{"ftSignatureFormat":65537,"ftSignatureType":4919338167972134933,"Caption":"<processType>","Data":"SonstigerVorgang"},{"ftSignatureFormat":65537,"ftSignatureType":4919338167972134932,"Caption":"<kassen-seriennummer>","Data":"JYn4Vlh0CUGq48vMNkFYbw"},{"ftSignatureFormat":65537,"ftSignatureType":4919338167972134934,"Caption":"<processData>","Data":"monthly-closing"},{"ftSignatureFormat":65537,"ftSignatureType":4919338167972134935,"Caption":"<transaktions-nummer>","Data":"3"},{"ftSignatureFormat":65537,"ftSignatureType":4919338167972134936,"Caption":"<signatur-zaehler>","Data":"228"},{"ftSignatureFormat":65537,"ftSignatureType":4919338167972134937,"Caption":"<start-zeit>","Data":"2024-07-29T10:05:34.000Z"},{"ftSignatureFormat":65537,"ftSignatureType":4919338167972134938,"Caption":"<log-time>","Data":"2024-07-29T10:05:35.000Z"},{"ftSignatureFormat":65537,"ftSignatureType":4919338167972134941,"Caption":"<signatur>","Data":"IhEUPZQwSJ3D2ZpUM9YXxJ4eApXInrCRqBuJhsBKq48uFyiHyQ7vbTD9KdnDI7KOugLhPHQbVBAHBrR+cxkfXw=="},{"ftSignatureFormat":65537,"ftSignatureType":4919338167972134939,"Caption":"<sig-alg>","Data":"ecdsa-plain-SHA256"},{"ftSignatureFormat":65537,"ftSignatureType":4919338167972134940,"Caption":"<log-time-format>","Data":"unixTime"},{"ftSignatureFormat":1,"ftSignatureType":4919338167972134946,"Caption":"<certification-id>","Data":"BSI-K-TR-0490-2021"},{"ftSignatureFormat":65537,"ftSignatureType":4919338167972134942,"Caption":"<public-key>","Data":"BCDbvZw4kY3/4DQDJPbqFgVEc/3Z9wwHWAk+dIMTbEwE/5N3Tj6sUhvMxXWD4z7PctM+AqHuAmAx4fxjYzdg7cA="}],"ftState":4919338167972134912,"ftStateData":"{\"MasterData\":{\"Account\":{\"AccountId\":\"21c4bf4f-f56e-414f-b90b-6156a4a94a38\",\"AccountName\":\"Erlebnishotel \\\"Zur Schiffsmühle\\\" GmbH              \",\"Street\":\"Zur Schiffsmühle 2\",\"Zip\":\"04668\",\"City\":\"Grimma\",\"Country\":\"DE\",\"TaxId\":null,\"VatId\":\"DE326822181\"},\"Outlet\":{\"OutletId\":\"b2c5714d-11c1-4b9d-8b25-8f9a4777d1f6\",\"OutletName\":\"Erlebnishotel \\\"Zur Schiffsmühle\\\" GmbH ASA\",\"Street\":\"Zur Schiffsmühle 2\",\"Zip\":\"04668\",\"City\":\"Grimma\",\"Country\":\"DE\",\"VatId\":\"DE326822181\",\"LocationId\":null},\"Agencies\":[],\"PosSystems\":[{\"PosSystemId\":\"18a8d87e-89a5-ea11-a812-000d3a49ee32\",\"Brand\":\"protel hotelsoftware\",\"Model\":\"Cloud\",\"SoftwareVersion\":null,\"BaseCurrency\":\"EUR\",\"Type\":null},{\"PosSystemId\":\"2989d51c-e7e5-ea11-a817-000d3a49ee32\",\"Brand\":\"ASA Hotel\",\"Model\":\"Hotelsoftware\",\"SoftwareVersion\":null,\"BaseCurrency\":\"EUR\",\"Type\":null},{\"PosSystemId\":\"688dbf72-89a5-ea11-a812-000d3a49ee32\",\"Brand\":\"protel hotelsoftware\",\"Model\":\"On Premise\",\"SoftwareVersion\":null,\"BaseCurrency\":\"EUR\",\"Type\":null}]}}"}""");


            requestHash2.Should().Be(queueItem2.requestHash);
            responseHash2.Should().Be(queueItem2.responseHash);

            var receiptHash2 = sut.GenerateBase64ChainHash(receiptJournal1.ftReceiptHash, receiptJournal2, queueItem2);

            receiptHash2.Should().Be(receiptJournal2.ftReceiptHash);
        }
    }
}
