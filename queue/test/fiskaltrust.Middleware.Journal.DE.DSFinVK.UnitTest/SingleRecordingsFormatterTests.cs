using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.Middleware.Journal.DE.Common.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.ModuleFormatters;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.UnitTest
{
    public class SingleRecordingsFormatterTests
    {
        [Fact]
        public async Task ProcessSingleRecordings_ShouldNot_CreateDuplicateBonPosEntries()
        {
            var parameters = new DSFinVKParameters();
            var sut = new SingleRecordingsFormatter(Mock.Of<ILogger>(), parameters);

            var outputDirectory = Guid.NewGuid().ToString();
            var testData = new List<ReceiptEntry>();
            var receiptRefData = new List<ReceiptReferencesGroupedData>();
            await sut.ProcessAsync(outputDirectory, testData, new DailyClosingParameters(), receiptRefData, null);
        }

    }
}
