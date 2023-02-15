using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AutoFixture;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Storage.AcceptanceTest
{
    public static class StorageTestFixtureProvider
    {
        public static Fixture GetFixture()
        {
            var fixture = new Fixture();
            fixture.Inject(DateTime.UtcNow);
            fixture.Customize<decimal>(c => c.FromFactory<int>(i => i * 1.33M));
            
            fixture.Customize<ftQueueItem>(c => c.With(r => r.request, JsonConvert.SerializeObject(fixture.Create<ReceiptRequest>())));
            
            fixture.Customize<ftJournalDE>(c => c
                .With(r => r.FileContentBase64, Convert.ToBase64String(Encoding.UTF8.GetBytes(fixture.Create<string>())))
                .With(r => r.FileExtension, ".zip")
                );

            fixture.Customize<ftActionJournal>(c => c.With(r => r.TimeStamp, DateTime.UtcNow.Ticks));
            
            return fixture;
        }
        public static async Task<List<ftJournalDE>> SetDifferentTimestamps(List<ftJournalDE> ftJournalDEs)
        {
            foreach(var ftJournalDE in ftJournalDEs)
            {
                ftJournalDE.TimeStamp= DateTime.UtcNow.Ticks;
                await Task.Delay(1).ConfigureAwait(false);
            }
            return ftJournalDEs;
        }
        public static async Task<List<ftJournalAT>> SetDifferentTimestamps(List<ftJournalAT> ftJournalATs)
        {
            foreach (var ftJournalAT in ftJournalATs)
            {
                ftJournalAT.TimeStamp = DateTime.UtcNow.Ticks;
                await Task.Delay(1).ConfigureAwait(false);
            }
            return ftJournalATs;
        }
        public static async Task<List<ftJournalFR>> SetDifferentTimestamps(List<ftJournalFR> ftJournalFRs)
        {
            foreach (var ftJournalFR in ftJournalFRs)
            {
                ftJournalFR.TimeStamp = DateTime.UtcNow.Ticks;
                await Task.Delay(1).ConfigureAwait(false);
            }
            return ftJournalFRs;
        }
        public static async Task<List<ftReceiptJournal>> SetDifferentTimestamps(List<ftReceiptJournal> ftReceiptJournals)
        {
            foreach (var ftReceiptJournal in ftReceiptJournals)
            {
                ftReceiptJournal.TimeStamp = DateTime.UtcNow.Ticks;
                await Task.Delay(1).ConfigureAwait(false);
            }
            return ftReceiptJournals;
        }
        public static async Task<List<ftActionJournal>> SetDifferentTimestamps(List<ftActionJournal> ftActionJournals)
        {
            foreach (var ftActionJournal in ftActionJournals)
            {
                ftActionJournal.TimeStamp = DateTime.UtcNow.Ticks;
                await Task.Delay(1).ConfigureAwait(false);
            }
            return ftActionJournals;
        }
        public static async Task<List<ftQueueItem>> SetDifferentTimestamps(List<ftQueueItem> ftQueueItems)
        {
            foreach (var ftQueueItem in ftQueueItems)
            {
                ftQueueItem.TimeStamp = DateTime.UtcNow.Ticks;
                await Task.Delay(1).ConfigureAwait(false);
            }
            return ftQueueItems;
        }
    }
}