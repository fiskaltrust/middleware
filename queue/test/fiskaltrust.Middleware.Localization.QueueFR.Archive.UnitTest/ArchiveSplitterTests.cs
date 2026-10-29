using System;
using System.Linq;
using fiskaltrust.Middleware.Localization.QueueFR.Archive.Model;
using fiskaltrust.Middleware.Localization.QueueFR.Archive.Split;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.Archive.UnitTest
{
    public class ArchiveSplitterTests
    {
        private static DateTime Utc(int y, int m, int d, int h = 0, int min = 0) => new DateTime(y, m, d, h, min, 0, DateTimeKind.Utc);

        private static System.Collections.Generic.IReadOnlyList<MainArchive<TestRecord>> Split(ArchivePeriod period, ArchiveSplitOptions? options, params TestRecord[] records)
            => ArchiveSplitter.Split(records, r => r.Moment, r => r.Key, period, options);

        [Fact]
        public void Slices_cut_at_calendar_year()
        {
            var slices = ArchiveSplitter.Slices(new ArchivePeriod(Utc(2023, 6, 1), Utc(2025, 3, 1)));

            slices.Should().Equal(
                new ArchivePeriod(Utc(2023, 6, 1), Utc(2024, 1, 1)),
                new ArchivePeriod(Utc(2024, 1, 1), Utc(2025, 1, 1)),
                new ArchivePeriod(Utc(2025, 1, 1), Utc(2025, 3, 1)));
        }

        [Fact]
        public void Slices_cut_at_a_July_fiscal_year()
        {
            var options = new ArchiveSplitOptions { FiscalYear = new FiscalYear(7, 1) };

            var slices = ArchiveSplitter.Slices(new ArchivePeriod(Utc(2024, 1, 1), Utc(2026, 1, 1)), options);

            slices.Select(s => s.Start).Should().Equal(Utc(2024, 1, 1), Utc(2024, 7, 1), Utc(2025, 7, 1));
            slices.Last().End.Should().Be(Utc(2026, 1, 1));
        }

        [Fact]
        public void Boundaries_follow_the_configured_time_zone()
        {
            var plusOne = TimeZoneInfo.CreateCustomTimeZone("Test+1", TimeSpan.FromHours(1), "Test+1", "Test+1");
            var options = new ArchiveSplitOptions { BoundaryTimeZone = plusOne };
            var lateEvening = new TestRecord { Moment = Utc(2024, 12, 31, 23, 30) }; // 00:30 on 1 January local

            var archives = Split(new ArchivePeriod(Utc(2024, 6, 1), Utc(2025, 6, 1)), options, lateEvening);

            archives.Should().HaveCount(2);
            archives[0].Period.End.Should().Be(Utc(2024, 12, 31, 23));
            archives[0].IsEmpty.Should().BeTrue();
            archives[1].SubArchives.Single().Records.Should().ContainSingle().Which.Should().BeSameAs(lateEvening);
        }

        [Fact]
        public void Request_over_years_without_records_gives_empty_archives_keyed_on_the_previous_key()
        {
            // market-fr#146: records in 2000 and 2005 only, request 2000-2005 -> six archives, four empty.
            var first = TestRecord.At(2000, 3, 1, "AAAA");
            var last = TestRecord.At(2005, 3, 1, "BBBB");

            var archives = Split(new ArchivePeriod(Utc(2000, 1, 1), Utc(2006, 1, 1)), null, first, last);

            archives.Should().HaveCount(6);
            archives.Count(a => a.IsEmpty).Should().Be(4);
            archives.Skip(1).Take(4).Should().OnlyContain(a => a.SubArchives.Count == 1 && a.SubArchives[0].Key.CertificateSerialNumber == "AAAA");
            archives[1].FirstRecordMoment.Should().BeNull();
            archives[5].SubArchives.Single().Key.CertificateSerialNumber.Should().Be("BBBB");
        }

        [Fact]
        public void Leading_empty_years_take_the_first_key_after_them()
        {
            var archives = Split(new ArchivePeriod(Utc(2020, 1, 1), Utc(2023, 1, 1)), null, TestRecord.At(2022, 5, 5, "CCCC"));

            archives.Should().HaveCount(3);
            archives[0].SubArchives.Single().Key.CertificateSerialNumber.Should().Be("CCCC");
            archives[0].SubArchives.Single().Records.Should().BeEmpty();
        }

        [Fact]
        public void Without_any_record_the_default_key_is_used_or_no_sub_archive_is_made()
        {
            var period = new ArchivePeriod(Utc(2024, 1, 1), Utc(2025, 1, 1));
            var key = new SubArchiveKey("DDDD", ArchiveFormatVersion.V2);

            Split(period, null).Single().SubArchives.Should().BeEmpty();
            Split(period, new ArchiveSplitOptions { DefaultKey = key }).Single().SubArchives.Single().Key.Should().Be(key);
        }

        [Fact]
        public void A_certificate_or_format_switch_splits_the_year_into_sub_archives_in_chain_order()
        {
            var records = new[]
            {
                TestRecord.At(2024, 1, 10, "AAAA", ArchiveFormatVersion.V1),
                TestRecord.At(2024, 2, 10, "AAAA", ArchiveFormatVersion.V1),
                TestRecord.At(2024, 3, 10, "AAAA", ArchiveFormatVersion.V2), // format switch
                TestRecord.At(2024, 9, 10, "bbbb", ArchiveFormatVersion.V2), // certificate renewal
                TestRecord.At(2024, 10, 10, "BBBB", ArchiveFormatVersion.V2) // serial compared case-insensitively
            };

            var archive = Split(new ArchivePeriod(Utc(2024, 1, 1), Utc(2025, 1, 1)), null, records).Single();

            archive.SubArchives.Select(s => s.Key.ToString()).Should().Equal("AAAA/v1", "AAAA/v2", "bbbb/v2");
            archive.SubArchives.Select(s => s.Records.Count).Should().Equal(2, 1, 2);
            archive.SubArchives[0].FirstRecordMoment.Should().Be(records[0].Moment);
            archive.SubArchives[0].LastRecordMoment.Should().Be(records[1].Moment);
            archive.FirstRecordMoment.Should().Be(records[0].Moment);
            archive.LastRecordMoment.Should().Be(records[4].Moment);
        }

        [Fact]
        public void Records_outside_the_request_are_excluded_and_the_end_is_exclusive()
        {
            var inside = TestRecord.At(2024, 6, 1);
            var before = TestRecord.At(2023, 12, 31);
            var atEnd = new TestRecord { Moment = Utc(2025, 1, 1) };

            var archive = Split(new ArchivePeriod(Utc(2024, 1, 1), Utc(2025, 1, 1)), null, before, inside, atEnd).Single();

            archive.SubArchives.Single().Records.Should().Equal(inside);
        }

        [Fact]
        public void Unspecified_moments_are_taken_as_utc()
        {
            var record = new TestRecord { Moment = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Unspecified) };

            var archives = Split(new ArchivePeriod(Utc(2024, 1, 1), Utc(2026, 1, 1)), null, record);

            archives[0].SubArchives.Single().Records.Should().ContainSingle();
            archives[0].FirstRecordMoment!.Value.Kind.Should().Be(DateTimeKind.Utc);
        }

        [Fact]
        public void Invalid_periods_and_fiscal_years_throw()
        {
            FluentActions.Invoking(() => new ArchivePeriod(Utc(2024, 1, 1), Utc(2024, 1, 1))).Should().Throw<ArgumentException>();
            FluentActions.Invoking(() => new FiscalYear(2, 29)).Should().Throw<ArgumentOutOfRangeException>();
            FluentActions.Invoking(() => new FiscalYear(13, 1)).Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}
