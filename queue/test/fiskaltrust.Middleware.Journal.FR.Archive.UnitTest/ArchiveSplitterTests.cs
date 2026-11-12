using System;
using System.Linq;
using fiskaltrust.Middleware.Journal.FR.Archive.Model;
using fiskaltrust.Middleware.Journal.FR.Archive.Split;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Journal.FR.Archive.UnitTest
{
    /// <summary>
    /// ADR 0019 and contract section 10.3: the records of a request, in queue order, cut into standalone files at a
    /// certificate change, at the half migration and at the one-year limit, each file stating its own slice.
    /// </summary>
    public class ArchiveSplitterTests
    {
        private static DateTime Utc(int year, int month, int day, int hour = 12, int minute = 0) => new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc);

        private static TestRecord R(DateTime moment, string serial = "0A1B", ArchiveFormatVersion? version = null, string chain = "T")
            => new TestRecord { Moment = moment, Serial = serial, Version = version ?? ArchiveFormatVersion.V2, Chain = chain };

        private static ArchivePeriod Period(DateTime from, DateTime to) => new ArchivePeriod(from, to);

        [Fact]
        public void A_request_within_one_certificate_and_a_year_is_one_file_with_exactly_the_requested_slice()
        {
            var from = Utc(2026, 1, 1, 0);
            var to = Utc(2026, 4, 1, 0);
            var records = new[] { R(Utc(2026, 1, 5)), R(Utc(2026, 2, 5), chain: "A"), R(Utc(2026, 3, 5)) };

            var files = ArchiveSplitter.Split(records, r => r.Moment, r => r.Key, Period(from, to));

            var file = files.Should().ContainSingle("an archive receipt inside the period is not a cut point").Subject;
            file.Records.Should().Equal(records);
            file.Period.Should().Be(Period(from, to));
            file.RequestedPeriod.Should().Be(Period(from, to));
            file.Part.Should().Be(1);
            file.PartCount.Should().Be(1);
            file.ClosedBy.Should().Be(ArchiveClosedBy.PeriodEnd);
            file.Key.Should().Be(new ArchiveFileKey("0A1B", ArchiveFormatVersion.V2));
            file.FirstRecord.Should().BeSameAs(records[0]);
            file.LastRecord.Should().BeSameAs(records[2]);
        }

        [Fact]
        public void Records_run_in_queue_order_from_the_first_at_or_after_the_start_to_the_last_before_the_end()
        {
            var from = Utc(2026, 1, 1, 0);
            var to = Utc(2026, 2, 1, 0);
            var before = R(Utc(2025, 12, 31));
            var first = R(Utc(2026, 1, 2));
            var jumped = R(Utc(2025, 12, 1), chain: "Z"); // a clock that jumped back: between the two, it stays in
            var last = R(Utc(2026, 1, 31));
            var after = R(Utc(2026, 2, 2));

            var files = ArchiveSplitter.Split(new[] { before, first, jumped, last, after }, r => r.Moment, r => r.Key, Period(from, to));

            files.Single().Records.Should().Equal(first, jumped, last);
        }

        [Fact]
        public void A_range_without_records_gives_no_file()
        {
            var records = new[] { R(Utc(2025, 1, 1)) };

            ArchiveSplitter.Split(records, r => r.Moment, r => r.Key, Period(Utc(2026, 1, 1), Utc(2026, 2, 1))).Should().BeEmpty();
        }

        [Fact]
        public void A_certificate_change_ends_the_file_on_the_last_entry_of_the_old_certificate()
        {
            var from = Utc(2026, 1, 1, 0);
            var to = Utc(2026, 7, 1, 0);
            var a = new[] { R(Utc(2026, 1, 10), "AAAA"), R(Utc(2026, 2, 10), "AAAA") };
            var b = new[] { R(Utc(2026, 3, 10), "BBBB"), R(Utc(2026, 4, 10), "BBBB") };

            var files = ArchiveSplitter.Split(a.Concat(b).ToList(), r => r.Moment, r => r.Key, Period(from, to));

            files.Select(f => f.Key.CertificateSerialNumber).Should().Equal("AAAA", "BBBB");
            files[0].Records.Should().Equal(a);
            files[0].ClosedBy.Should().Be(ArchiveClosedBy.LastSignedEntry);
            files[0].Period.Should().Be(Period(from, b[0].Moment), "periodEnd is the next file's first record");
            files[1].Period.Should().Be(Period(b[0].Moment, to));
            files[1].ClosedBy.Should().Be(ArchiveClosedBy.PeriodEnd);
            files.Select(f => (f.Part, f.PartCount)).Should().Equal(((int?) 1, (int?) 2), ((int?) 2, (int?) 2));
        }

        [Fact]
        public void An_SCU_switched_out_and_back_in_gives_one_file_per_stretch()
        {
            var records = new[] { R(Utc(2026, 1, 10), "AAAA"), R(Utc(2026, 2, 10), "BBBB"), R(Utc(2026, 3, 10), "AAAA") };

            var files = ArchiveSplitter.Split(records, r => r.Moment, r => r.Key, Period(Utc(2026, 1, 1), Utc(2026, 4, 1)));

            files.Select(f => f.Key.CertificateSerialNumber).Should().Equal("AAAA", "BBBB", "AAAA");
        }

        [Fact]
        public void The_half_migration_ends_the_file_before_the_first_v2_entry()
        {
            var v1 = R(Utc(2026, 1, 10), version: ArchiveFormatVersion.V1);
            var v2 = R(Utc(2026, 2, 10));

            var files = ArchiveSplitter.Split(new[] { v1, v2 }, r => r.Moment, r => r.Key, Period(Utc(2026, 1, 1), Utc(2026, 3, 1)));

            files.Select(f => f.Key.FormatVersion).Should().Equal(ArchiveFormatVersion.V1, ArchiveFormatVersion.V2);
            files[0].ClosedBy.Should().Be(ArchiveClosedBy.FormatChange);
        }

        [Fact]
        public void The_year_limit_cuts_at_the_first_record_plus_a_year_at_midnight_as_in_the_contract_example()
        {
            // Contract section 10.3: 2025-01-01 to 2026-02-01, first record on 2025-01-01 at 08:00,
            // gives 2025-01-01 to 2026-01-01, then 2026-01-01 to 2026-02-01.
            var from = Utc(2025, 1, 1, 0);
            var to = Utc(2026, 2, 1, 0);
            var first = R(Utc(2025, 1, 1, 8));
            var lastOfYear = R(new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc));
            var nextYear = R(Utc(2026, 1, 1, 0, 30));

            var files = ArchiveSplitter.Split(new[] { first, lastOfYear, nextYear }, r => r.Moment, r => r.Key, Period(from, to));

            files.Should().HaveCount(2);
            files[0].Records.Should().Equal(first, lastOfYear);
            files[0].ClosedBy.Should().Be(ArchiveClosedBy.YearLimit);
            files[0].Period.Should().Be(Period(from, Utc(2026, 1, 1, 0)));
            files[1].Records.Should().Equal(nextYear);
            files[1].Period.Should().Be(Period(Utc(2026, 1, 1, 0), to));
        }

        [Fact]
        public void The_limit_counts_again_from_each_files_first_record()
        {
            var records = new[] { R(Utc(2024, 3, 1)), R(Utc(2025, 3, 2)), R(Utc(2026, 3, 1)), R(Utc(2026, 3, 3)) };

            var files = ArchiveSplitter.Split(records, r => r.Moment, r => r.Key, Period(Utc(2024, 1, 1), Utc(2026, 6, 1)));

            files.Select(f => f.Records.Count).Should().Equal(1, 2, 1);
            files.Should().OnlyContain(f => f.Period.End <= f.Period.Start.AddYears(1));
            files[1].Period.End.Should().Be(Utc(2026, 3, 2, 0), "limit of the second file: 2025-03-02 plus a year, at midnight");
        }

        [Fact]
        public void A_slice_starts_on_its_first_record_when_starting_on_the_request_would_exceed_a_year()
        {
            var first = R(Utc(2025, 6, 1, 9));

            var file = ArchiveSplitter.Split(new[] { first }, r => r.Moment, r => r.Key, Period(Utc(2024, 1, 1, 0), Utc(2026, 1, 1, 0))).Single();

            file.Period.Should().Be(Period(first.Moment, Utc(2026, 1, 1, 0)));
            file.RequestedPeriod.Should().Be(Period(Utc(2024, 1, 1, 0), Utc(2026, 1, 1, 0)));
        }

        [Fact]
        public void A_clock_set_back_at_a_certificate_change_ends_the_slice_one_tick_after_its_last_record()
        {
            var lastA = R(Utc(2026, 2, 10, 12), "AAAA");
            var firstB = R(Utc(2026, 2, 10, 11), "BBBB"); // the new SCU's clock is an hour behind

            var files = ArchiveSplitter.Split(new[] { R(Utc(2026, 1, 10), "AAAA"), lastA, firstB }, r => r.Moment, r => r.Key, Period(Utc(2026, 1, 1), Utc(2026, 3, 1)));

            files[0].Period.End.Should().Be(lastA.Moment.AddTicks(1));
            files[1].Period.Start.Should().Be(firstB.Moment, "the next file starts on its own first record, so the slices overlap");
        }

        [Fact]
        public void A_record_range_gives_a_fixed_file_bounded_by_its_records()
        {
            var records = new[] { R(Utc(2025, 1, 5)), R(Utc(2025, 6, 5), chain: "Z"), R(Utc(2025, 12, 5)), R(Utc(2026, 1, 6)) };

            var file = ArchiveSplitter.Range(records, r => r.Id, records[0].Id, records[2].Id, r => r.Moment, r => r.Key, forcedArchive: "fa-1");

            file.Records.Should().Equal(records.Take(3));
            file.Period.Should().BeNull("a fixed file is bounded by records, not dates");
            file.RequestedPeriod.Should().BeNull();
            file.Part.Should().BeNull();
            file.ClosedBy.Should().Be(ArchiveClosedBy.YearLimit);
            file.ForcedArchive.Should().Be("fa-1");
            file.LastRecordMoment.Should().Be(records[2].Moment);
        }

        [Fact]
        public void A_record_range_that_crosses_a_cut_or_the_year_limit_or_names_no_record_is_refused()
        {
            var a = R(Utc(2025, 1, 5), "AAAA");
            var b = R(Utc(2025, 2, 5), "BBBB");
            var late = R(Utc(2026, 1, 5), "AAAA");
            var records = new[] { a, b, late };

            FluentActions.Invoking(() => ArchiveSplitter.Range(records, r => r.Id, a.Id, b.Id, r => r.Moment, r => r.Key)).Should().Throw<ArgumentException>().WithMessage("*cut*");
            FluentActions.Invoking(() => ArchiveSplitter.Range(new[] { a, late }, r => r.Id, a.Id, late.Id, r => r.Moment, r => r.Key)).Should().Throw<ArgumentException>().WithMessage("*year limit*");
            FluentActions.Invoking(() => ArchiveSplitter.Range(records, r => r.Id, a.Id, Guid.NewGuid(), r => r.Moment, r => r.Key)).Should().Throw<ArgumentException>().WithMessage("*not among*");
            FluentActions.Invoking(() => ArchiveSplitter.Range(records, r => r.Id, b.Id, a.Id, r => r.Moment, r => r.Key)).Should().Throw<ArgumentException>().WithMessage("*before it starts*");
        }

        [Fact]
        public void Unspecified_moments_are_taken_as_utc()
        {
            var record = R(new DateTime(2026, 1, 5, 12, 0, 0, DateTimeKind.Unspecified));

            var file = ArchiveSplitter.Split(new[] { record }, r => r.Moment, r => r.Key, Period(Utc(2026, 1, 1), Utc(2026, 2, 1))).Single();

            file.FirstRecordMoment.Kind.Should().Be(DateTimeKind.Utc);
        }

        [Fact]
        public void The_year_limit_is_the_first_moment_plus_a_year_at_utc_midnight()
            => ArchiveSplitter.YearLimit(new DateTime(2025, 1, 1, 8, 0, 0, DateTimeKind.Utc)).Should().Be(Utc(2026, 1, 1, 0));
    }
}
