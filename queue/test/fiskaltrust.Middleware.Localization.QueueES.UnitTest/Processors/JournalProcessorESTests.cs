using System.Net.Mime;
using System.Text;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.es.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueES.Models;
using fiskaltrust.Middleware.Localization.QueueES.Processors;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.storage.V0;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueES.UnitTest.Processors;

public class JournalProcessorESTests
{
    private readonly Mock<IMiddlewareJournalESRepository> _journalESRepositoryMock;
    private readonly AsyncLazy<IMiddlewareJournalESRepository> _journalESRepository;
    private readonly JournalProcessorES _journalProcessor;

    public JournalProcessorESTests()
    {
        _journalESRepositoryMock = new Mock<IMiddlewareJournalESRepository>();
        _journalESRepository = new AsyncLazy<IMiddlewareJournalESRepository>(() => Task.FromResult(_journalESRepositoryMock.Object));
        _journalProcessor = new JournalProcessorES(_journalESRepository);
    }

    [Fact]
    public async Task ProcessAsync_VeriFactuJournalType_ReturnsJsonContentType()
    {
        // Arrange
        var request = new JournalRequest
        {
            ftJournalType = JournalTypeES.VeriFactu.As<JournalType>(),
            From = 123456789,
            To = 987654321
        };

        var veriFactuJournals = new List<ftJournalES>
        {
            new()
            {
                ftJournalESId = Guid.NewGuid(),
                JournalType = JournalESType.VeriFactu.ToString(),
                Data = "<VeriFactu><TestData>Sample</TestData></VeriFactu>"
            }
        }.ToAsyncEnumerable();

        _journalESRepositoryMock.Setup(x => x.GetByTimeStampRangeAsync(request.From, request.To))
            .Returns(veriFactuJournals);

        // Act
        var (contentType, result) = _journalProcessor.ProcessAsync(request);
        var jsonData = await ReadDataAsync(result);

        // Assert
        Assert.Equal(MediaTypeNames.Application.Json, contentType.MediaType);
        Assert.Equal(Encoding.UTF8.WebName, contentType.CharSet);
        Assert.NotNull(result);
        Assert.NotEmpty(jsonData);
        _journalESRepositoryMock.Verify(x => x.GetByTimeStampRangeAsync(request.From, request.To), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_VeriFactuJournalType_ReturnsCorrectJsonData()
    {
        // Arrange
        var request = new JournalRequest
        {
            ftJournalType = JournalTypeES.VeriFactu.As<JournalType>(),
            From = 123456789,
            To = 987654321
        };

        var expectedJsonData = JsonSerializer.Serialize(new GovernmentAPI { Request = "<VeriFactu><Valid>Request</Valid></VeriFactu>", Response = "<VeriFactu><Valid>Result</Valid></VeriFactu>", Version = "V0" });
        var veriFactuJournals = new List<ftJournalES>
        {
            new()
            {
                ftJournalESId = Guid.NewGuid(),
                JournalType = JournalESType.VeriFactu.ToString(),
                Data = expectedJsonData
            }
        }.ToAsyncEnumerable();

        _journalESRepositoryMock.Setup(x => x.GetByTimeStampRangeAsync(request.From, request.To))
            .Returns(veriFactuJournals);

        // Act
        var (contentType, result) = _journalProcessor.ProcessAsync(request);
        var jsonData = await ReadDataAsync(result);

        // Assert
        Assert.Equal($"[{expectedJsonData}]", jsonData);
    }

    [Fact]
    public async Task ProcessAsync_MultipleVeriFactuEntries_ReturnsAllJsonData()
    {
        // Arrange
        var request = new JournalRequest
        {
            ftJournalType = JournalTypeES.VeriFactu.As<JournalType>(),
            From = 123456789,
            To = 987654321
        };

        var jsonData1 = JsonSerializer.Serialize(new GovernmentAPI { Request = "<VeriFactu><Valid>Request1</Valid></VeriFactu>", Response = "<VeriFactu><Valid>Result1</Valid></VeriFactu>", Version = "V0" });
        var jsonData2 = JsonSerializer.Serialize(new GovernmentAPI { Request = "<VeriFactu><Valid>Request2</Valid></VeriFactu>", Response = "<VeriFactu><Valid>Result2</Valid></VeriFactu>", Version = "V0" });
        var veriFactuJournals = new List<ftJournalES>
        {
            new()
            {
                ftJournalESId = Guid.NewGuid(),
                JournalType = JournalESType.VeriFactu.ToString(),
                Data = jsonData1
            },
            new()
            {
                ftJournalESId = Guid.NewGuid(),
                JournalType = JournalESType.VeriFactu.ToString(),
                Data = jsonData2
            }
        }.ToAsyncEnumerable();

        _journalESRepositoryMock.Setup(x => x.GetByTimeStampRangeAsync(request.From, request.To))
            .Returns(veriFactuJournals);

        // Act
        var (contentType, result) = _journalProcessor.ProcessAsync(request);
        var jsonData = await ReadDataAsync(result);

        // Assert
        Assert.Contains(jsonData1, jsonData);
        Assert.Contains(jsonData2, jsonData);
    }

    [Fact]
    public async Task ProcessAsync_MixedJournalTypes_ReturnsOnlyVeriFactuData()
    {
        // Arrange
        var request = new JournalRequest
        {
            ftJournalType = JournalTypeES.VeriFactu.As<JournalType>(),
            From = 123456789,
            To = 987654321
        };

        var veriFactuData = JsonSerializer.Serialize(new GovernmentAPI { Request = "<VeriFactu><Valid>Request</Valid></VeriFactu>", Response = "<VeriFactu><Valid>Result</Valid></VeriFactu>", Version = "V0" });
        var otherData = "<Other><InvalidData>Should be excluded</InvalidData></Other>";
        var journals = new List<ftJournalES>
        {
            new()
            {
                ftJournalESId = Guid.NewGuid(),
                JournalType = JournalESType.VeriFactu.ToString(),
                Data = veriFactuData
            },
            new()
            {
                ftJournalESId = Guid.NewGuid(),
                JournalType = "Other",
                Data = otherData
            }
        }.ToAsyncEnumerable();

        _journalESRepositoryMock.Setup(x => x.GetByTimeStampRangeAsync(request.From, request.To))
            .Returns(journals);

        // Act
        var (contentType, result) = _journalProcessor.ProcessAsync(request);
        var jsonData = await ReadDataAsync(result);

        // Assert
        Assert.Contains(veriFactuData, jsonData);
        Assert.DoesNotContain(otherData, jsonData);
    }

    [Fact]
    public async Task ProcessAsync_NoVeriFactuEntries_ReturnsEmptyData()
    {
        // Arrange
        var request = new JournalRequest
        {
            ftJournalType = JournalTypeES.VeriFactu.As<JournalType>(),
            From = 123456789,
            To = 987654321
        };

        var journals = new List<ftJournalES>
        {
            new()
            {
                ftJournalESId = Guid.NewGuid(),
                JournalType = "Other",
                Data = "<Other>Non-VeriFactu data</Other>"
            }
        }.ToAsyncEnumerable();

        _journalESRepositoryMock.Setup(x => x.GetByTimeStampRangeAsync(request.From, request.To))
            .Returns(journals);

        // Act
        var (contentType, result) = _journalProcessor.ProcessAsync(request);
        var jsonData = await ReadDataAsync(result);

        // Assert
        Assert.Equal("[]", jsonData);
    }

    [Fact]
    public async Task ProcessAsync_EmptyJournalList_ReturnsEmptyData()
    {
        // Arrange
        var request = new JournalRequest
        {
            ftJournalType = JournalTypeES.VeriFactu.As<JournalType>(),
            From = 123456789,
            To = 987654321
        };

        var emptyJournals = new List<ftJournalES>().ToAsyncEnumerable();

        _journalESRepositoryMock.Setup(x => x.GetByTimeStampRangeAsync(request.From, request.To))
            .Returns(emptyJournals);

        // Act
        var (contentType, result) = _journalProcessor.ProcessAsync(request);
        var jsonData = await ReadDataAsync(result);

        // Assert
        Assert.Equal("[]", jsonData);
    }

    [Fact]
    public void ProcessAsync_UnsupportedJournalType_ThrowsException()
    {
        // Arrange
        var request = new JournalRequest
        {
            ftJournalType = JournalType.ActionJournal,
            From = 123456789,
            To = 987654321
        };

        // Act & Assert
        var exception = Assert.Throws<Exception>(() => _journalProcessor.ProcessAsync(request));
        Assert.Contains("Unsupported journal type", exception.Message);
        Assert.Contains(request.ftJournalType.ToString(), exception.Message);
    }

    [Fact]
    public async Task ProcessAsync_InvalidJournalType_FiltersOutInvalidEntries()
    {
        // Arrange
        var request = new JournalRequest
        {
            ftJournalType = JournalTypeES.VeriFactu.As<JournalType>(),
            From = 123456789,
            To = 987654321
        };

        var validData = JsonSerializer.Serialize(new GovernmentAPI { Request = "<VeriFactu><Valid>Request</Valid></VeriFactu>", Response = "<VeriFactu><Valid>Result</Valid></VeriFactu>", Version = "V0" });
        var journals = new List<ftJournalES>
        {
            new()
            {
                ftJournalESId = Guid.NewGuid(),
                JournalType = JournalESType.VeriFactu.ToString(),
                Data = validData
            },
            new()
            {
                ftJournalESId = Guid.NewGuid(),
                JournalType = "InvalidEnumValue",
                Data = "<Invalid>Should be filtered</Invalid>"
            }
        }.ToAsyncEnumerable();

        _journalESRepositoryMock.Setup(x => x.GetByTimeStampRangeAsync(request.From, request.To))
            .Returns(journals);

        // Act
        var (contentType, result) = _journalProcessor.ProcessAsync(request);
        var jsonData = await ReadDataAsync(result);

        // Assert
        Assert.Equal($"[{validData}]", jsonData);
    }

    [Fact]
    public async Task ProcessAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        var request = new JournalRequest
        {
            ftJournalType = JournalTypeES.VeriFactu.As<JournalType>(),
            From = 123456789,
            To = 987654321
        };

        var expectedException = new Exception("Repository error");
        _journalESRepositoryMock.Setup(x => x.GetByTimeStampRangeAsync(request.From, request.To))
            .Throws(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(async () =>
        {
            var (contentType, result) = _journalProcessor.ProcessAsync(request);
            await ReadDataAsync(result); // This will trigger the repository call
        });

        Assert.Equal(expectedException, exception);
    }

    [Theory]
    [InlineData(0, 1000)]
    [InlineData(500, 1500)]
    [InlineData(999999, 0)]
    [InlineData(999999, -20)]
    public async Task ProcessAsync_DifferentTimeRanges_CallsRepositoryWithCorrectParameters(long from, long to)
    {
        // Arrange
        var request = new JournalRequest
        {
            ftJournalType = JournalTypeES.VeriFactu.As<JournalType>(),
            From = from,
            To = to
        };

        var emptyJournals = new List<ftJournalES>().ToAsyncEnumerable();
        _journalESRepositoryMock.Setup(x => x.GetByTimeStampRangeAsync(from, to))
            .Returns(emptyJournals);
        _journalESRepositoryMock.Setup(x => x.GetEntriesOnOrAfterTimeStampAsync(from, -(int) to))
            .Returns(emptyJournals);
        _journalESRepositoryMock.Setup(x => x.GetEntriesOnOrAfterTimeStampAsync(from, null))
            .Returns(emptyJournals);

        // Act
        var (contentType, result) = _journalProcessor.ProcessAsync(request);
        await ReadDataAsync(result); // Consume the result to trigger repository call

        // Assert
        if (to > 0)
        {
            _journalESRepositoryMock.Verify(x => x.GetByTimeStampRangeAsync(from, to), Times.Once);
        }
        else if (to == 0)
        {
            _journalESRepositoryMock.Verify(x => x.GetEntriesOnOrAfterTimeStampAsync(from, null), Times.Once);
        }
        else
        {
            _journalESRepositoryMock.Verify(x => x.GetEntriesOnOrAfterTimeStampAsync(from, -(int) to), Times.Once);
        }
    }

    [Fact]
    public async Task ProcessAsync_LargeDataSet_HandlesMultipleEntriesCorrectly()
    {
        // Arrange
        var request = new JournalRequest
        {
            ftJournalType = JournalTypeES.VeriFactu.As<JournalType>(),
            From = 123456789,
            To = 987654321
        };

        var largeJournalSet = Enumerable.Range(0, 100)
            .Select(i => new ftJournalES
            {
                ftJournalESId = Guid.NewGuid(),
                JournalType = JournalESType.VeriFactu.ToString(),
                Data = JsonSerializer.Serialize(new GovernmentAPI { Request = $"<VeriFactu><Valid>Request{i}</Valid></VeriFactu>", Response = $"<VeriFactu><Valid>Result{i}</Valid></VeriFactu>", Version = "V0" }, new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping })
            }).ToAsyncEnumerable();

        _journalESRepositoryMock.Setup(x => x.GetByTimeStampRangeAsync(request.From, request.To))
            .Returns(largeJournalSet);

        // Act
        var (contentType, result) = _journalProcessor.ProcessAsync(request);
        var jsonData = await ReadDataAsync(result);

        // Assert
        for (int i = 0; i < 100; i++)
        {
            Assert.Contains($"<Valid>Request{1}</Valid>", jsonData);
        }
    }

    private async Task<string> ReadDataAsync(IAsyncEnumerable<byte[]> dataStream)
    {
        var result = new StringBuilder();
        await foreach (var chunk in dataStream)
        {
            result.Append(Encoding.UTF8.GetString(chunk));
        }
        return result.ToString();
    }
}
