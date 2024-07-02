using System;
using System.Text;
using AutoFixture;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Storage.AcceptanceTest
{
    public static class StorageTestFixtureProvider
    {
        public static Fixture GetFixture()
        {
            var fixture = new Fixture();
            fixture.Customize<DateTime>(c => c.FromFactory(() => new Fixture().Create<DateTime>().ToUniversalTime()));
            fixture.Customize<decimal>(c => c.FromFactory<int>(i => i * 1.33M));
            
            fixture.Customize<ftQueueItem>(c => c
                .With(r => r.request, JsonConvert.SerializeObject(fixture.Create<ReceiptRequest>()))
                .With(r => r.response, JsonConvert.SerializeObject(fixture.Create<ReceiptResponse>()))
            );
            fixture.Customize<FailedFinishTransaction>(c => c.With(r => r.Request, JsonConvert.SerializeObject(fixture.Create<ReceiptRequest>())));
            fixture.Customize<FailedStartTransaction>(c => c.With(r => r.Request, JsonConvert.SerializeObject(fixture.Create<ReceiptRequest>())));

            fixture.Customize<ftJournalDE>(c => c
                .With(r => r.FileContentBase64, Convert.ToBase64String(Encoding.UTF8.GetBytes(fixture.Create<string>())))
                .With(r => r.FileExtension, ".zip")
                );
            
            return fixture;
        }
    }
}