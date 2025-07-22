using System.Buffers;
using System.IO.Pipelines;
using System.Net.Mime;
using System.Text;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest;

public class JournalProcessorTests
{
    private readonly Mock<IStorageProvider> _storageProviderMock;
    private readonly Mock<IJournalProcessor> _marketSpecificJournalProcessorMock;
    private readonly Mock<IConfigurationRepository> _configurationRepositoryMock;
    private readonly Mock<IMiddlewareQueueItemRepository> _queueItemRepositoryMock;
    private readonly Mock<IMiddlewareReceiptJournalRepository> _receiptJournalRepositoryMock;
    private readonly Mock<IMiddlewareActionJournalRepository> _actionJournalRepositoryMock;
    private readonly Mock<ILogger<JournalProcessor>> _loggerMock;
    private readonly Dictionary<string, object> _configuration;
    private readonly JournalProcessor _journalProcessor;

    public JournalProcessorTests()
    {
        _storageProviderMock = new Mock<IStorageProvider>();
        _marketSpecificJournalProcessorMock = new Mock<IJournalProcessor>();
        _configurationRepositoryMock = new Mock<IConfigurationRepository>();
        _queueItemRepositoryMock = new Mock<IMiddlewareQueueItemRepository>();
        _receiptJournalRepositoryMock = new Mock<IMiddlewareReceiptJournalRepository>();
        _actionJournalRepositoryMock = new Mock<IMiddlewareActionJournalRepository>();
        _loggerMock = new Mock<ILogger<JournalProcessor>>();
        _configuration = new Dictionary<string, object>();

        SetupStorageProvider();
        _journalProcessor = new JournalProcessor(
            _storageProviderMock.Object,
            _marketSpecificJournalProcessorMock.Object,
            _configuration,
            _loggerMock.Object);
    }

    private void SetupStorageProvider()
    {
        var configRepoTask = Task.FromResult(_configurationRepositoryMock.Object);
        var queueItemRepoTask = Task.FromResult(_queueItemRepositoryMock.Object);
        var receiptJournalRepoTask = Task.FromResult(_receiptJournalRepositoryMock.Object);
        var actionJournalRepoTask = Task.FromResult(_actionJournalRepositoryMock.Object);

        _storageProviderMock.Setup(x => x.CreateConfigurationRepository())
            .Returns(new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(_configurationRepositoryMock.Object)));
        _storageProviderMock.Setup(x => x.CreateMiddlewareQueueItemRepository())
            .Returns(new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(_queueItemRepositoryMock.Object)));
        _storageProviderMock.Setup(x => x.CreateMiddlewareReceiptJournalRepository())
            .Returns(new AsyncLazy<IMiddlewareReceiptJournalRepository>(() => Task.FromResult(_receiptJournalRepositoryMock.Object)));
        _storageProviderMock.Setup(x => x.CreateMiddlewareActionJournalRepository())
            .Returns(new AsyncLazy<IMiddlewareActionJournalRepository>(() => Task.FromResult(_actionJournalRepositoryMock.Object)));
    }

    [Fact]
    public async Task ProcessAsync_ActionJournal_ReturnsCorrectContentTypeAndData()
    {
        // Arrange
        var request = new JournalRequest { ftJournalType = JournalType.ActionJournal, From = 123456789, To = 0 };
        var expectedGuid = Guid.NewGuid();
        var actionJournals = new List<ftActionJournal>
        {
            new() { ftQueueItemId = expectedGuid, ftActionJournalId = Guid.NewGuid() }
        }.ToAsyncEnumerable();

        _actionJournalRepositoryMock.Setup(x => x.GetEntriesOnOrAfterTimeStampAsync(It.IsAny<long>(), It.IsAny<int?>()))
            .Returns(actionJournals);

        // Act
        var (contentType, pipeReader) = await _journalProcessor.ProcessAsync(request);
        var data = await ReadAndDeserializeAsync<List<ftActionJournal>>(pipeReader);

        // Assert
        Assert.Equal("application/json", contentType.MediaType);
        Assert.Equal("utf-8", contentType.CharSet);
        Assert.NotNull(data);
        Assert.Single(data);
        Assert.Equal(expectedGuid, data[0].ftQueueItemId);
    }

    [Fact]
    public async Task ProcessAsync_ReceiptJournal_ReturnsCorrectContentTypeAndData()
    {
        // Arrange
        var request = new JournalRequest { ftJournalType = JournalType.ReceiptJournal, From = 123456789, To = 987654321 };
        var expectedGuid = Guid.NewGuid();
        var receiptJournals = new List<ftReceiptJournal>
        {
            new() { ftQueueItemId = expectedGuid, ftReceiptJournalId = Guid.NewGuid() }
        }.ToAsyncEnumerable();

        _receiptJournalRepositoryMock.Setup(x => x.GetByTimeStampRangeAsync(It.IsAny<long>(), It.IsAny<long>()))
            .Returns(receiptJournals);

        // Act
        var (contentType, pipeReader) = await _journalProcessor.ProcessAsync(request);
        var data = await ReadAndDeserializeAsync<List<ftReceiptJournal>>(pipeReader);

        // Assert
        Assert.Equal("application/json", contentType.MediaType);
        Assert.Equal("utf-8", contentType.CharSet);
        Assert.NotNull(data);
        Assert.Single(data);
        Assert.Equal(expectedGuid, data[0].ftQueueItemId);
    }

    [Fact]
    public async Task ProcessAsync_QueueItem_ReturnsCorrectContentTypeAndData()
    {
        // Arrange
        var request = new JournalRequest { ftJournalType = JournalType.QueueItem, From = 123456789, To = -10 };
        var expectedGuid = Guid.NewGuid();
        var queueItems = new List<ftQueueItem>
        {
            new() { ftQueueItemId = expectedGuid, ftQueueId = Guid.NewGuid() }
        }.ToAsyncEnumerable();

        _queueItemRepositoryMock.Setup(x => x.GetEntriesOnOrAfterTimeStampAsync(It.IsAny<long>(), It.IsAny<int?>()))
            .Returns(queueItems);

        // Act
        var (contentType, pipeReader) = await _journalProcessor.ProcessAsync(request);
        var data = await ReadAndDeserializeAsync<List<ftQueueItem>>(pipeReader);

        // Assert
        Assert.Equal("application/json", contentType.MediaType);
        Assert.Equal("utf-8", contentType.CharSet);
        Assert.NotNull(data);
        Assert.Single(data);
        Assert.Equal(expectedGuid, data[0].ftQueueItemId);
        _queueItemRepositoryMock.Verify(x => x.GetEntriesOnOrAfterTimeStampAsync(123456789, 10), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_Configuration_ReturnsConfigurationData()
    {
        // Arrange
        var request = new JournalRequest { ftJournalType = JournalType.Configuration };
        SetupConfigurationRepository();

        // Act
        var (contentType, pipeReader) = await _journalProcessor.ProcessAsync(request);
        var jsonString = await ReadPipeReaderAsStringAsync(pipeReader);
        var isValidJson = IsValidJson(jsonString);

        // Assert
        Assert.Equal("application/json", contentType.MediaType);
        Assert.Equal("utf-8", contentType.CharSet);
        Assert.True(isValidJson, "Configuration data should be valid JSON");
        Assert.Contains("CashBoxList", jsonString);
        Assert.Contains("QueueList", jsonString);
        _configurationRepositoryMock.Verify(x => x.GetCashBoxListAsync(), Times.Once);
        _configurationRepositoryMock.Verify(x => x.GetQueueListAsync(), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_UnknownJournalType_ReturnsAssemblyInfo()
    {
        // Arrange
        var request = new JournalRequest { ftJournalType = (JournalType) 999 };

        // Act
        var (contentType, pipeReader) = await _journalProcessor.ProcessAsync(request);
        var jsonString = await ReadPipeReaderAsStringAsync(pipeReader);
        var isValidJson = IsValidJson(jsonString);

        // Assert
        Assert.Equal("application/json", contentType.MediaType);
        Assert.Equal("utf-8", contentType.CharSet);
        Assert.True(isValidJson, "Assembly info should be valid JSON");
        Assert.Contains("Assembly", jsonString);
    }

    [Fact]
    public async Task ProcessAsync_MarketSpecificJournalType_CallsMarketSpecificProcessor()
    {
        // Arrange
        var request = new JournalRequest { ftJournalType = (JournalType) 0x4154000000000001 }; // AT market
        var expectedContentType = new ContentType("application/xml");
        var expectedData = new[] { Encoding.UTF8.GetBytes("<xml>test</xml>") }.ToAsyncEnumerable();

        _marketSpecificJournalProcessorMock.Setup(x => x.ProcessAsync(It.IsAny<JournalRequest>()))
            .Returns((expectedContentType, expectedData));

        // Act
        var (contentType, pipeReader) = await _journalProcessor.ProcessAsync(request);

        // Assert
        Assert.Equal(expectedContentType, contentType);
        Assert.NotNull(pipeReader);
        _marketSpecificJournalProcessorMock.Verify(x => x.ProcessAsync(request), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_ConfigurationWithDictionaryData_ReturnsConfigurationFromDictionary()
    {
        // Arrange
        var request = new JournalRequest { ftJournalType = JournalType.Configuration };
        var queueESList = new List<ftQueueES> { new() { ftQueueESId = Guid.NewGuid() } };
        _configuration["init_ftQueueES"] = JsonConvert.SerializeObject(queueESList);
        SetupConfigurationRepository();

        // Act
        var (contentType, pipeReader) = await _journalProcessor.ProcessAsync(request);
        var jsonString = await ReadPipeReaderAsStringAsync(pipeReader);

        // Assert
        Assert.Equal("application/json", contentType.MediaType);
        Assert.True(IsValidJson(jsonString), "Configuration with dictionary data should be valid JSON");
        Assert.Contains("QueueESList", jsonString);
    }

    [Fact]
    public async Task ProcessAsync_RepositoryThrowsException_LogsErrorAndRethrows()
    {
        // Arrange
        var request = new JournalRequest { ftJournalType = JournalType.ActionJournal, From = 123456789, To = 0 };
        var expectedException = new Exception("Repository error");

        _actionJournalRepositoryMock.Setup(x => x.GetEntriesOnOrAfterTimeStampAsync(It.IsAny<long>(), It.IsAny<int?>()))
            .Throws(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => _journalProcessor.ProcessAsync(request));
        Assert.Equal(expectedException, exception);
    }

    [Theory]
    [InlineData(123456789, 0)] // To = 0, should call GetEntriesOnOrAfterTimeStampAsync without take
    [InlineData(123456789, -5)] // To < 0, should call GetEntriesOnOrAfterTimeStampAsync with take
    [InlineData(123456789, 987654321)] // To > 0, should call GetByTimeStampRangeAsync
    public async Task ProcessAsync_DifferentTimeRanges_CallsCorrectRepositoryMethod(long from, long to)
    {
        // Arrange
        var request = new JournalRequest { ftJournalType = JournalType.ActionJournal, From = from, To = to };
        var actionJournals = new List<ftActionJournal>().ToAsyncEnumerable();

        if (to < 0)
        {
            _actionJournalRepositoryMock.Setup(x => x.GetEntriesOnOrAfterTimeStampAsync(from, (int) -to))
                .Returns(actionJournals);
        }
        else if (to == 0)
        {
            _actionJournalRepositoryMock.Setup(x => x.GetEntriesOnOrAfterTimeStampAsync(from, It.IsAny<int?>()))
                .Returns(actionJournals);
        }
        else
        {
            _actionJournalRepositoryMock.Setup(x => x.GetByTimeStampRangeAsync(from, to))
                .Returns(actionJournals);
        }

        // Act
        await _journalProcessor.ProcessAsync(request);

        // Assert
        if (to < 0)
        {
            _actionJournalRepositoryMock.Verify(x => x.GetEntriesOnOrAfterTimeStampAsync(from, (int) -to), Times.Once);
        }
        else if (to == 0)
        {
            _actionJournalRepositoryMock.Verify(x => x.GetEntriesOnOrAfterTimeStampAsync(from, It.IsAny<int?>()), Times.Once);
        }
        else
        {
            _actionJournalRepositoryMock.Verify(x => x.GetByTimeStampRangeAsync(from, to), Times.Once);
        }
    }

    [Fact]
    public async Task ProcessAsync_TempFileCleanup_DeletesTempFileAfterProcessing()
    {
        // Arrange
        var request = new JournalRequest { ftJournalType = JournalType.ActionJournal, From = 123456789, To = 0 };
        var actionJournals = new List<ftActionJournal>
        {
            new() { ftQueueItemId = Guid.NewGuid(), ftActionJournalId = Guid.NewGuid() }
        }.ToAsyncEnumerable();

        _actionJournalRepositoryMock.Setup(x => x.GetEntriesOnOrAfterTimeStampAsync(It.IsAny<long>(), It.IsAny<int?>()))
            .Returns(actionJournals);

        var tempFilesBefore = Directory.GetFiles(Path.GetTempPath(), "tmp*").Length;

        // Act
        var (contentType, pipeReader) = await _journalProcessor.ProcessAsync(request);

        // Read all data from pipe to ensure processing completes
        var buffer = new byte[4096];
        while (await pipeReader.ReadAsync() is var result && !result.IsCompleted)
        {
            pipeReader.AdvanceTo(result.Buffer.End);
        }

        // Give some time for cleanup task to complete
        await Task.Delay(100);

        // Assert
        var tempFilesAfter = Directory.GetFiles(Path.GetTempPath(), "tmp*").Length;
        Assert.True(tempFilesAfter <= tempFilesBefore, "Temp file should be cleaned up after processing");
    }

    [Fact]
    public async Task ProcessAsync_ExceptionDuringProcessing_StillCleansTempFile()
    {
        // Arrange
        var request = new JournalRequest { ftJournalType = JournalType.ActionJournal, From = 123456789, To = 0 };
        var expectedException = new Exception("Processing error");

        _actionJournalRepositoryMock.Setup(x => x.GetEntriesOnOrAfterTimeStampAsync(It.IsAny<long>(), It.IsAny<int?>()))
            .Throws(expectedException);

        var tempFilesBefore = Directory.GetFiles(Path.GetTempPath(), "tmp*").Length;

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _journalProcessor.ProcessAsync(request));

        // Give some time for cleanup to complete
        await Task.Delay(100);

        var tempFilesAfter = Directory.GetFiles(Path.GetTempPath(), "tmp*").Length;
        Assert.True(tempFilesAfter <= tempFilesBefore, "Temp file should be cleaned up even when exception occurs");
    }

    [Fact]
    public async Task ProcessAsync_MultipleSimultaneousCalls_HandlesMultipleTempFiles()
    {
        // Arrange
        var request1 = new JournalRequest { ftJournalType = JournalType.ActionJournal, From = 123456789, To = 0 };
        var request2 = new JournalRequest { ftJournalType = JournalType.ReceiptJournal, From = 123456789, To = 0 };

        var actionJournals = new List<ftActionJournal>
        {
            new() { ftQueueItemId = Guid.NewGuid(), ftActionJournalId = Guid.NewGuid() }
        }.ToAsyncEnumerable();

        var receiptJournals = new List<ftReceiptJournal>
        {
            new() { ftQueueItemId = Guid.NewGuid(), ftReceiptJournalId = Guid.NewGuid() }
        }.ToAsyncEnumerable();

        _actionJournalRepositoryMock.Setup(x => x.GetEntriesOnOrAfterTimeStampAsync(It.IsAny<long>(), It.IsAny<int?>()))
            .Returns(actionJournals);
        _receiptJournalRepositoryMock.Setup(x => x.GetEntriesOnOrAfterTimeStampAsync(It.IsAny<long>(), It.IsAny<int?>()))
            .Returns(receiptJournals);

        var tempFilesBefore = Directory.GetFiles(Path.GetTempPath(), "tmp*").Length;

        // Act
        var task1 = _journalProcessor.ProcessAsync(request1);
        var task2 = _journalProcessor.ProcessAsync(request2);

        var results = await Task.WhenAll(task1, task2);

        // Read from both pipes to ensure processing completes
        var buffer = new byte[4096];
        foreach (var (_, pipeReader) in results)
        {
            while (await pipeReader.ReadAsync() is var result && !result.IsCompleted)
            {
                pipeReader.AdvanceTo(result.Buffer.End);
            }
        }

        // Give some time for cleanup tasks to complete
        await Task.Delay(200);

        // Assert
        var tempFilesAfter = Directory.GetFiles(Path.GetTempPath(), "tmp*").Length;
        Assert.True(tempFilesAfter <= tempFilesBefore, "All temp files should be cleaned up after concurrent processing");
    }

    [Fact]
    public async Task ProcessAsync_PipeReaderDisposed_DoesNotLeakTempFiles()
    {
        // Arrange
        var request = new JournalRequest { ftJournalType = JournalType.Configuration };
        SetupConfigurationRepository();

        var tempFilesBefore = Directory.GetFiles(Path.GetTempPath(), "tmp*").Length;

        // Act
        var (contentType, pipeReader) = await _journalProcessor.ProcessAsync(request);

        // Dispose pipe reader without reading (simulating early disposal)
        await pipeReader.CompleteAsync();

        // Give some time for cleanup task to complete
        await Task.Delay(100);

        // Assert
        var tempFilesAfter = Directory.GetFiles(Path.GetTempPath(), "tmp*").Length;
        Assert.True(tempFilesAfter <= tempFilesBefore, "Temp file should be cleaned up even when pipe reader is disposed early");
    }

    [Fact]
    public async Task ProcessAsync_LargeDataSet_HandlesTempFileCorrectly()
    {
        // Arrange
        var request = new JournalRequest { ftJournalType = JournalType.ActionJournal, From = 123456789, To = -1000 };
        var largeActionJournals = Enumerable.Range(0, 1000)
            .Select(i => new ftActionJournal
            {
                ftQueueItemId = Guid.NewGuid(),
                ftActionJournalId = Guid.NewGuid(),
                Type = $"Large test data item {i} with lots of content to ensure temp file handling works with larger datasets"
            }).ToAsyncEnumerable();

        _actionJournalRepositoryMock.Setup(x => x.GetEntriesOnOrAfterTimeStampAsync(It.IsAny<long>(), It.IsAny<int?>()))
            .Returns(largeActionJournals).Verifiable();

        var tempFilesBefore = Directory.GetFiles(Path.GetTempPath(), "tmp*").Length;

        // Act
        var (contentType, pipeReader) = await _journalProcessor.ProcessAsync(request);
        var data = await ReadAndDeserializeAsync<List<ftActionJournal>>(pipeReader);

        // Give some time for cleanup task to complete
        await Task.Delay(100);

        // Assert
        _actionJournalRepositoryMock.Verify();
        Assert.NotNull(data);
        Assert.Equal(1000, data.Count);
        Assert.All(data, item => Assert.Contains("Large test data item", item.Type));
        var tempFilesAfter = Directory.GetFiles(Path.GetTempPath(), "tmp*").Length;
        Assert.True(tempFilesAfter <= tempFilesBefore, "Temp file should be cleaned up after processing large dataset");
    }

    private void SetupConfigurationRepository()
    {
        _configurationRepositoryMock.Setup(x => x.GetCashBoxListAsync())
            .ReturnsAsync(new List<ftCashBox>());
        _configurationRepositoryMock.Setup(x => x.GetQueueListAsync())
            .ReturnsAsync(new List<ftQueue>());
        _configurationRepositoryMock.Setup(x => x.GetQueueATListAsync())
            .ReturnsAsync(new List<ftQueueAT>());
        _configurationRepositoryMock.Setup(x => x.GetQueueDEListAsync())
            .ReturnsAsync(new List<ftQueueDE>());
        _configurationRepositoryMock.Setup(x => x.GetQueueFRListAsync())
            .ReturnsAsync(new List<ftQueueFR>());
        _configurationRepositoryMock.Setup(x => x.GetQueueITListAsync())
            .ReturnsAsync(new List<ftQueueIT>());
        _configurationRepositoryMock.Setup(x => x.GetQueueMEListAsync())
            .ReturnsAsync(new List<ftQueueME>());
        _configurationRepositoryMock.Setup(x => x.GetSignaturCreationUnitATListAsync())
            .ReturnsAsync(new List<ftSignaturCreationUnitAT>());
        _configurationRepositoryMock.Setup(x => x.GetSignaturCreationUnitDEListAsync())
            .ReturnsAsync(new List<ftSignaturCreationUnitDE>());
        _configurationRepositoryMock.Setup(x => x.GetSignaturCreationUnitFRListAsync())
            .ReturnsAsync(new List<ftSignaturCreationUnitFR>());
        _configurationRepositoryMock.Setup(x => x.GetSignaturCreationUnitITListAsync())
            .ReturnsAsync(new List<ftSignaturCreationUnitIT>());
        _configurationRepositoryMock.Setup(x => x.GetSignaturCreationUnitMEListAsync())
            .ReturnsAsync(new List<ftSignaturCreationUnitME>());
    }

    private async Task<T> ReadAndDeserializeAsync<T>(PipeReader pipeReader)
    {
        var jsonString = await ReadPipeReaderAsStringAsync(pipeReader);
        return JsonConvert.DeserializeObject<T>(jsonString);
    }

    private async Task<string> ReadPipeReaderAsStringAsync(PipeReader pipeReader)
    {
        var stringBuilder = new StringBuilder();
        while (true)
        {
            var result = await pipeReader.ReadAsync();
            var buffer = result.Buffer;

            if (buffer.IsEmpty && result.IsCompleted)
                break;

            foreach (var segment in buffer)
            {
                stringBuilder.Append(Encoding.UTF8.GetString(segment.Span));
            }

            pipeReader.AdvanceTo(buffer.End);

            if (result.IsCompleted)
                break;
        }

        await pipeReader.CompleteAsync();
        return stringBuilder.ToString();
    }

    private bool IsValidJson(string jsonString)
    {
        try
        {
            JsonConvert.DeserializeObject(jsonString);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
