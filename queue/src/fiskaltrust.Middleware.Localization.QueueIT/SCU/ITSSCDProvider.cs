using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Localization.QueueIT.Models;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueIT.SCU;

/// <summary>
/// Talks to the <see cref="IITSSCD"/> (ifPOS.v1) client the launcher provides through the
/// <see cref="IClientFactory{T}"/>. The client is created on first use from the <c>ftSignaturCreationUnitIT</c>
/// assigned to the queue; when creating it fails, the next call tries again instead of caching the failure.
/// </summary>
public class ITSSCDProvider : IITSSCDProvider
{
    private readonly ILogger<ITSSCDProvider> _logger;
    private readonly IClientFactory<IITSSCD> _clientFactory;
    private readonly AsyncLazy<IConfigurationRepository> _configurationRepository;
    private readonly Guid _queueId;
    private readonly QueueITConfiguration _queueConfiguration;

    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private IITSSCD? _instance;

    public ITSSCDProvider(ILogger<ITSSCDProvider> logger, IClientFactory<IITSSCD> clientFactory, AsyncLazy<IConfigurationRepository> configurationRepository, Guid queueId, QueueITConfiguration queueConfiguration)
    {
        _logger = logger;
        _clientFactory = clientFactory;
        _configurationRepository = configurationRepository;
        _queueId = queueId;
        _queueConfiguration = queueConfiguration;
    }

    public async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request)
    {
        var client = await GetClientAsync().ConfigureAwait(false);
        var response = await client.ProcessReceiptAsync(new ifPOS.v1.it.ProcessRequest
        {
            ReceiptRequest = ITSSCDContractConverter.ToV1(request.ReceiptRequest),
            ReceiptResponse = ITSSCDContractConverter.ToV1(request.ReceiptResponse),
        }).ConfigureAwait(false);

        return new ProcessResponse
        {
            ReceiptResponse = ITSSCDContractConverter.ToV2(response.ReceiptResponse, request.ReceiptResponse)
        };
    }

    public async Task<RTInfo> GetRTInfoAsync()
    {
        var client = await GetClientAsync().ConfigureAwait(false);
        return await client.GetRTInfoAsync().ConfigureAwait(false);
    }

    private async Task<IITSSCD> GetClientAsync()
    {
        if (_instance is not null)
        {
            return _instance;
        }

        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            _instance ??= await CreateClientAsync().ConfigureAwait(false);
            return _instance;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<IITSSCD> CreateClientAsync()
    {
        var configurationRepository = await _configurationRepository;
        var queueIT = await configurationRepository.GetQueueITAsync(_queueId).ConfigureAwait(false);
        if (queueIT?.ftSignaturCreationUnitITId is null)
        {
            throw new InvalidOperationException(ErrorMessagesIT.NoSignaturCreationUnitAssigned(_queueId));
        }

        var signaturCreationUnitIT = await configurationRepository.GetSignaturCreationUnitITAsync(queueIT.ftSignaturCreationUnitITId.Value).ConfigureAwait(false)
            ?? throw new InvalidOperationException(ErrorMessagesIT.SignaturCreationUnitNotFound(queueIT.ftSignaturCreationUnitITId.Value));

        var uri = GetUriForSignaturCreationUnit(signaturCreationUnitIT);
        var config = new ClientConfiguration
        {
            Url = uri.ToString(),
            UrlType = uri.Scheme
        };
        if (_queueConfiguration.ScuTimeoutMs.HasValue)
        {
            config.Timeout = TimeSpan.FromMilliseconds(_queueConfiguration.ScuTimeoutMs.Value);
        }
        if (_queueConfiguration.ScuMaxRetries.HasValue)
        {
            config.RetryCount = _queueConfiguration.ScuMaxRetries.Value;
        }

        var client = _clientFactory.CreateClient(config);
        try
        {
            var rtInfo = await client.GetRTInfoAsync().ConfigureAwait(false);
            signaturCreationUnitIT.InfoJson = JsonConvert.SerializeObject(rtInfo);
            await configurationRepository.InsertOrUpdateSignaturCreationUnitITAsync(signaturCreationUnitIT).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update the status of the SCU (Url: {ScuUrl}, Id: {ScuId}). Will try again later...", config.Url, signaturCreationUnitIT.ftSignaturCreationUnitITId);
        }
        return client;
    }

    /// <summary>
    /// The SCU url is either a plain url or a JSON array of urls; when several are configured the gRPC one is preferred.
    /// </summary>
    internal static Uri GetUriForSignaturCreationUnit(ftSignaturCreationUnitIT signaturCreationUnit)
    {
        var url = signaturCreationUnit.Url;
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException(ErrorMessagesIT.SignaturCreationUnitWithoutUrl(signaturCreationUnit.ftSignaturCreationUnitITId));
        }
        try
        {
            var urls = JsonConvert.DeserializeObject<string[]>(url);
            if (urls is { Length: > 0 })
            {
                url = urls.FirstOrDefault(x => x.StartsWith("grpc://")) ?? urls[0];
            }
        }
        catch (JsonException) { }
        return new Uri(url);
    }
}
