using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using fiskaltrust.Middleware.Localization.QueueFR.Archive.Csv;
using fiskaltrust.Middleware.Localization.QueueFR.Archive.Model;
using fiskaltrust.Middleware.Localization.QueueFR.Archive.Writing;

namespace fiskaltrust.Middleware.Localization.QueueFR.Archive.UnitTest
{
    public sealed class TestLine
    {
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    /// <summary>A stand-in for the queue item / receipt the real column sets will read (market-fr#227).</summary>
    public sealed class TestRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime Moment { get; set; }
        public string Chain { get; set; } = "T";
        public string Serial { get; set; } = "0A1B";
        public ArchiveFormatVersion Version { get; set; } = ArchiveFormatVersion.V2;
        public decimal Total { get; set; }
        public string Text { get; set; } = string.Empty;
        public List<TestLine> Lines { get; set; } = new List<TestLine>();

        public SubArchiveKey Key => new SubArchiveKey(Serial, Version);

        public static TestRecord At(int year, int month, int day, string serial = "0A1B", ArchiveFormatVersion? version = null)
            => new TestRecord { Moment = new DateTime(year, month, day, 12, 0, 0, DateTimeKind.Utc), Serial = serial, Version = version ?? ArchiveFormatVersion.V2 };
    }

    internal static class TestFixtures
    {
        public static CsvSubArchiveWriter<TestRecord> Writer(ArchiveFormatVersion version) => new CsvSubArchiveWriter<TestRecord>(version, new ICsvFileDefinition<TestRecord>[]
        {
            new FlatCsvFile<TestRecord>("journal-T.csv", new[]
            {
                new CsvColumn<TestRecord>("ftQueueItemID", r => r.Id),
                new CsvColumn<TestRecord>("ftReceiptMoment", r => r.Moment),
                new CsvColumn<TestRecord>("Totalizer", r => r.Total),
                new CsvColumn<TestRecord>("Text", r => r.Text)
            }, r => r.Chain == "T"),
            new FlatCsvFile<TestRecord>("journal-G.csv", new[]
            {
                new CsvColumn<TestRecord>("ftQueueItemID", r => r.Id),
                new CsvColumn<TestRecord>("Totalizer", r => r.Total)
            }, r => r.Chain == "G"),
            new OneToManyCsvFile<TestRecord, TestLine>("charge-items.csv", r => r.Lines, new[]
            {
                new CsvColumn<ChildRow<TestRecord, TestLine>>("ftQueueItemID", row => row.Parent.Id),
                new CsvColumn<ChildRow<TestRecord, TestLine>>("Position", row => row.Index + 1),
                new CsvColumn<ChildRow<TestRecord, TestLine>>("Description", row => row.Child.Description),
                new CsvColumn<ChildRow<TestRecord, TestLine>>("Amount", row => row.Child.Amount)
            })
        });

        public static X509Certificate2 RsaCertificate()
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest("CN=fiskaltrust archive test RSA", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        }

        public static X509Certificate2 EcdsaCertificate()
        {
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var request = new CertificateRequest("CN=fiskaltrust archive test ECDSA", ecdsa, HashAlgorithmName.SHA256);
            return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        }
    }
}
