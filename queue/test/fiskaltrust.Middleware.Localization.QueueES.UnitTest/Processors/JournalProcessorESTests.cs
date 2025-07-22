using System.Net.Mime;
using System.Text;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.es.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueES.Processors;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.storage.V0;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueES.UnitTest.Processors;

public class JournalProcessorESTests
{
    private readonly Mock<IMiddlewareRepository<ftJournalES>> _journalESRepositoryMock;
    private readonly AsyncLazy<IMiddlewareRepository<ftJournalES>> _journalESRepository;
    private readonly JournalProcessorES _journalProcessor;

    public JournalProcessorESTests()
    {
        _journalESRepositoryMock = new Mock<IMiddlewareRepository<ftJournalES>>();
        _journalESRepository = new AsyncLazy<IMiddlewareRepository<ftJournalES>>(() => Task.FromResult(_journalESRepositoryMock.Object));
        _journalProcessor = new JournalProcessorES(_journalESRepository);
    }

    [Fact]
    public async Task ProcessAsync_VeriFactuJournalType_ReturnsXmlContentType()
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
        var xmlData = await ReadDataAsync(result);

        // Assert
        Assert.Equal(MediaTypeNames.Application.Xml, contentType.MediaType);
        Assert.Equal(Encoding.UTF8.WebName, contentType.CharSet);
        Assert.NotNull(result);
        Assert.NotEmpty(xmlData);
        _journalESRepositoryMock.Verify(x => x.GetByTimeStampRangeAsync(request.From, request.To), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_VeriFactuJournalType_ReturnsCorrectXmlData()
    {
        // Arrange
        var request = new JournalRequest
        {
            ftJournalType = JournalTypeES.VeriFactu.As<JournalType>(),
            From = 123456789,
            To = 987654321
        };

        var expectedXmlData = "<VeriFactu><TestData>Sample VeriFactu Data</TestData></VeriFactu>";
        var veriFactuJournals = new List<ftJournalES>
        {
            new()
            {
                ftJournalESId = Guid.NewGuid(),
                JournalType = JournalESType.VeriFactu.ToString(),
                Data = expectedXmlData
            }
        }.ToAsyncEnumerable();

        _journalESRepositoryMock.Setup(x => x.GetByTimeStampRangeAsync(request.From, request.To))
            .Returns(veriFactuJournals);

        // Act
        var (contentType, result) = _journalProcessor.ProcessAsync(request);
        var xmlData = await ReadDataAsync(result);

        // Assert
        Assert.Equal(expectedXmlData, xmlData);
    }

    [Fact]
    public async Task ProcessAsync_MultipleVeriFactuEntries_ReturnsAllXmlData()
    {
        // Arrange
        var request = new JournalRequest
        {
            ftJournalType = JournalTypeES.VeriFactu.As<JournalType>(),
            From = 123456789,
            To = 987654321
        };

        var xmlData1 = "<VeriFactu><Entry>1</Entry></VeriFactu>";
        var xmlData2 = "<VeriFactu><Entry>2</Entry></VeriFactu>";
        var veriFactuJournals = new List<ftJournalES>
        {
            new()
            {
                ftJournalESId = Guid.NewGuid(),
                JournalType = JournalESType.VeriFactu.ToString(),
                Data = xmlData1
            },
            new()
            {
                ftJournalESId = Guid.NewGuid(),
                JournalType = JournalESType.VeriFactu.ToString(),
                Data = xmlData2
            }
        }.ToAsyncEnumerable();

        _journalESRepositoryMock.Setup(x => x.GetByTimeStampRangeAsync(request.From, request.To))
            .Returns(veriFactuJournals);

        // Act
        var (contentType, result) = _journalProcessor.ProcessAsync(request);
        var xmlData = await ReadDataAsync(result);

        // Assert
        Assert.Contains(xmlData1, xmlData);
        Assert.Contains(xmlData2, xmlData);
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

        var veriFactuData = "<VeriFactu><ValidData>Should be included</ValidData></VeriFactu>";
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
        var xmlData = await ReadDataAsync(result);

        // Assert
        Assert.Contains(veriFactuData, xmlData);
        Assert.DoesNotContain(otherData, xmlData);
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
        var xmlData = await ReadDataAsync(result);

        // Assert
        Assert.Equal(string.Empty, xmlData);
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
        var xmlData = await ReadDataAsync(result);

        // Assert
        Assert.Equal(string.Empty, xmlData);
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

        var validData = "<VeriFactu><Valid>Data</Valid></VeriFactu>";
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
        var xmlData = await ReadDataAsync(result);

        // Assert
        Assert.Equal(validData, xmlData);
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

        // Act
        var (contentType, result) = _journalProcessor.ProcessAsync(request);
        await ReadDataAsync(result); // Consume the result to trigger repository call

        // Assert
        _journalESRepositoryMock.Verify(x => x.GetByTimeStampRangeAsync(from, to), Times.Once);
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
                Data = $"<VeriFactu><Entry>{i}</Entry></VeriFactu>"
            }).ToAsyncEnumerable();

        _journalESRepositoryMock.Setup(x => x.GetByTimeStampRangeAsync(request.From, request.To))
            .Returns(largeJournalSet);

        // Act
        var (contentType, result) = _journalProcessor.ProcessAsync(request);
        var xmlData = await ReadDataAsync(result);

        // Assert
        for (int i = 0; i < 100; i++)
        {
            Assert.Contains($"<Entry>{i}</Entry>", xmlData);
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
