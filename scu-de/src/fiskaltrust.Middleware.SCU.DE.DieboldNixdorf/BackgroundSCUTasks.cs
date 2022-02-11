using System;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Commands;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models.Enums;
using Microsoft.Extensions.Logging;
using Timer = System.Threading.Timer;

namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf
{
    public class BackgroundSCUTasks : IDisposable
    {
        private bool disposed = false;

        private readonly SemaphoreSlim timerSemaphore = new SemaphoreSlim(1, 1);
        private const int maxTimerSemaphoreWaitTimeout = 2 * 60 * 1000;

        private TimeSpan _timeUntilNextSelfTest = TimeSpan.FromHours(24);

        private readonly ILogger<BackgroundSCUTasks> _logger;
        private readonly TseCommunicationCommandHelper _tseCommunicationHelper;
        private readonly Timer _selfTestTimer;
        private readonly UtilityTseCommandsProvider _utilityTseCommandProvider;
        private readonly MaintenanceTseCommandProvider _maintenanceTseCommandProvider;
        private readonly StandardTseCommandsProvider _standardTseCommandsProvider;
        private readonly ExportTseCommandsProvider _exportTseCommandsProvider;

        public BackgroundSCUTasks(
            ILogger<BackgroundSCUTasks> logger,
            TseCommunicationCommandHelper tseCommunicationCommandHelper,
            UtilityTseCommandsProvider utilityTseCommandsProvider,
            MaintenanceTseCommandProvider maintenanceTseCommandProvider,
            StandardTseCommandsProvider standardTseCommandsProvider,
            ExportTseCommandsProvider exportTseCommandsProvider)
        {
            _logger = logger;

            _tseCommunicationHelper = tseCommunicationCommandHelper;
            _utilityTseCommandProvider = utilityTseCommandsProvider;
            _maintenanceTseCommandProvider = maintenanceTseCommandProvider;
            _standardTseCommandsProvider = standardTseCommandsProvider;
            _exportTseCommandsProvider = exportTseCommandsProvider;

            _selfTestTimer = new Timer(SelfTestCallback, null, 1000, Timeout.Infinite);
        }

        private void SelfTestCallback(object state) => ExecuteSelfTestLogic().Wait();

        public async Task ExecuteSelfTestLogic()
        {
            var locked = false;
            try
            {
                _standardTseCommandsProvider.DisableAsb();
                locked = await timerSemaphore.WaitAsync(maxTimerSemaphoreWaitTimeout);
                if (!locked)
                {
                    throw new Exception($"Unable to perform lock after {maxTimerSemaphoreWaitTimeout} ms");
                }
                MfcStatus mfcState = null;
                try
                {       
                    // 2020-10-06 Stefan Kert: If the selftest fails we do probably have a problem with an export, so we do reset the export 
                    _exportTseCommandsProvider.ResetExport();
                    mfcState = _tseCommunicationHelper.GetMfcStatus();
                }
                catch (Exception x)
                {
                    _logger.LogDebug(x, "Failed to fetch MfcState");
                    var selfTestResult = _maintenanceTseCommandProvider.RunSelfTest("DN TSEProduction ef82abcedf");
                    _logger.LogDebug("Executed SelfTest with SelfTestResult {0}", selfTestResult);
                    mfcState = _tseCommunicationHelper.GetMfcStatus();
                    ResetTimings();
                    return;
                }

                var timeUntilNextSelfTest = _utilityTseCommandProvider.GetTimeUntilNextSelfTest();
                if (TimeSpan.FromSeconds(timeUntilNextSelfTest) < TimeSpan.FromHours(12))
                {
                    if (!mfcState.IsInitialized)
                    {
                        var selfTestResult = _maintenanceTseCommandProvider.RunSelfTest("DN TSEProduction ef82abcedf");
                        _logger.LogDebug("Executed SelfTest with SelfTestResult {0}", selfTestResult);
                    }
                    else
                    {
                        var selfTestResult = _maintenanceTseCommandProvider.RunSelfTest();
                        if (selfTestResult == SelfTestResult.ClientNotRegistered)
                        {
                            selfTestResult = _maintenanceTseCommandProvider.RunSelfTest("DN TSEProduction ef82abcedf");
                            _logger.LogDebug("Executed SelfTest with SelfTestResult {0}", selfTestResult);
                        }
                    }
                }
                ResetTimings();
            }
            catch (Exception ex)
            {
                // 2020-10-06 Stefan Kert: If the selftest fails we do probably have a problem with an export, so we do reset the export 
                _exportTseCommandsProvider.ResetExport();
                _logger.LogError(ex, "Failed to Execute SelfTest");

                try
                {
                    var countryInfo = _standardTseCommandsProvider.GetCountryInfo();
                    var firmwareIdentifciation = "";
                    if (countryInfo.HardwareId == DieboldNixdorfHardwareId.SingleTSE)
                    {
                        var firmwareInfo = _standardTseCommandsProvider.GetFirmwareInfoForSingleTSE();
                        firmwareIdentifciation = $@"Firmware Version (SingleTSE): {firmwareInfo.FwMajorVersion}.{firmwareInfo.FwMinorVersion}.{firmwareInfo.FwBuildNo}
Loader Version: {firmwareInfo.LdrMajorVersion}.{firmwareInfo.LdrMinorVersion}.{firmwareInfo.LdrBuildNo}";
                    }
                    else if (countryInfo.HardwareId == DieboldNixdorfHardwareId.TSEConnectBox)
                    {
                        var firmwareInfo = _standardTseCommandsProvider.GetFirmwareInfoForConnectBox();
                        firmwareIdentifciation = $@"App Version (ConnectBox): {firmwareInfo.AppMajorVersion}.{firmwareInfo.AppMinorVersion}.{firmwareInfo.AppBuildNo}
OsUpd Version: {firmwareInfo.OsUpdMajorVersion}.{firmwareInfo.OsUpdMinorVersion}.{firmwareInfo.OsUpdBuildVersion}
OsMain Version: {firmwareInfo.OsMainMajorVersion}.{firmwareInfo.OsMainMinorVersion}.{firmwareInfo.OsMainBuildNo}";
                    }
                    _logger.LogDebug("Additional Hardware Information");
                    _logger.LogDebug("FirmwareInfo: {0}", firmwareIdentifciation);
                    _logger.LogDebug("ApiVersion: {0}.{1}", countryInfo.ApiMajorVersion, countryInfo.ApiMinorVersion);
                }
                catch { }

                _selfTestTimer.Change(TimeSpan.FromSeconds(5), Timeout.InfiniteTimeSpan);
            }
            finally
            {
                if (locked)
                {
                    timerSemaphore.Release(1);
                }
            }
        }

        private void ResetTimings()
        {
            var timeUntilNextSelfTest = _utilityTseCommandProvider.GetTimeUntilNextSelfTest();
            var time = TimeSpan.FromSeconds(timeUntilNextSelfTest * 0.9);
            _timeUntilNextSelfTest = time;
            _selfTestTimer.Change(_timeUntilNextSelfTest, _timeUntilNextSelfTest);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    _selfTestTimer.Dispose();
                }
                disposed = true;
            }
        }
    }
}
