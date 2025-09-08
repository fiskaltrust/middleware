param (
    [string]$OutputPath = "configuration.json",
    [string]$Package,
    [string]$cashBoxId,
    [string]$queueId,
    [string]$Version = "0.0.0-ci"
)

# Generate IDs
$scuId     = [guid]::NewGuid().ToString()
$timestamp = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()

# Build config object
$config = @{
    helpers = @()
    ftCashBoxId = $cashBoxId
    ftSignaturCreationDevices = @()
    ftQueues = @(
        @{
            Id = $queueId
            Package = $Package
            Version = $Version
            Configuration = @{
                init_ftQueue = @(
                    @{
                        ftQueueId = $queueId
                        ftCashBoxId = $cashBoxId
                        ftCurrentRow = 0
                        ftQueuedRow = 0
                        ftReceiptNumerator = 0
                        ftReceiptTotalizer = 0.0
                        ftReceiptHash = $null
                        StartMoment = $null
                        StopMoment = $null
                        CountryCode = "DE"
                        Timeout = 1500
                        TimeStamp = $timestamp
                    }
                )
                init_ftQueueDE = @(
                    @{
                        ftQueueDEId = $queueId
                        ftSignaturCreationUnitDEId = $scuId
                        LastHash = $null
                        CashBoxIdentification = "fdsgfg"
                        SSCDFailCount = 0
                        SSCDFailMoment = $null
                        SSCDFailQueueItemId = $null
                        UsedFailedCount = 0
                        UsedFailedMomentMin = $null
                        UsedFailedMomentMax = $null
                        UsedFailedQueueItemId = $null
                        DailyClosingNumber = 0
                        TimeStamp = $timestamp
                    }
                )
                init_ftCashBox = @{
                    ftCashBoxId = $cashBoxId
                    TimeStamp = $timestamp
                }
            }
        url = @("rest://localhost:1500/$queueId")
    }
)
TimeStamp = $timestamp
}

# Save file
$config | ConvertTo-Json -Depth 10 | Set-Content $OutputPath -Encoding UTF8

Write-Host "✅ Generated new configuration.json with queueId=$queueId, cashBoxId=$cashBoxId"
