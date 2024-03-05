﻿using System;
using fiskaltrust.ifPOS.v1.at;
using fiskaltrust.Middleware.Queue.Test.Launcher.Helpers;
using fiskaltrust.Middleware.SCU.AT.InMemory;
using fiskaltrust.Middleware.SCU.AT.Test.Launcher.Helpers;
using fiskaltrust.storage.serialization.V0;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;


var _useHelipad = false;
var _cashBoxId = "";
var _accessToken = "";
var _configurationFilePath = "C:\\Temp\\ATLauncher\\configuration.json";
var serviceFolder = "";


ftCashBoxConfiguration cashBoxConfiguration;
if (_useHelipad)
{
    cashBoxConfiguration = HelipadHelper.GetConfigurationAsync(_cashBoxId, _accessToken).Result;
}
else if (!string.IsNullOrEmpty(_configurationFilePath))
{
    cashBoxConfiguration = JsonConvert.DeserializeObject<ftCashBoxConfiguration>(File.ReadAllText(_configurationFilePath));
}
else
{
    throw new Exception("No configuration file or helipad is set.");
}

var config = cashBoxConfiguration.ftSignaturCreationDevices[0];
config.Configuration.Add("cashboxid", cashBoxConfiguration.ftCashBoxId);
config.Configuration.Add("accesstoken", "");
config.Configuration.Add("useoffline", true);
config.Configuration.Add("sandbox", true);
config.Configuration.Add("servicefolder", serviceFolder);
config.Configuration.Add("configuration", JsonConvert.SerializeObject(cashBoxConfiguration));

var serviceCollection = new ServiceCollection();
serviceCollection.AddStandardLoggers(LogLevel.Debug);

if (config.Package == "fiskaltrust.Middleware.SCU.AT.InMemory")
{
    ConfigureIMemory(config, serviceCollection);
}
else
{
    throw new NotSupportedException($"The given package {config.Package} is not supported.");
}
var provider = serviceCollection.BuildServiceProvider();

var atsscd = provider.GetRequiredService<IATSSCD>();
HostingHelper.SetupServiceForObject(config, atsscd, provider.GetRequiredService<ILoggerFactory>());

Console.WriteLine("Press key to end program");
Console.ReadLine();


static void ConfigureIMemory(PackageConfiguration queue, ServiceCollection serviceCollection)
{
    var bootStrapper = new ScuBootstrapper
    {
        Id = queue.Id,
        Configuration = queue.Configuration
    };
    bootStrapper.ConfigureServices(serviceCollection);
}

