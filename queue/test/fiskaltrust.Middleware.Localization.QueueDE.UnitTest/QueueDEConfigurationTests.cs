using System;
using System.Collections.Generic;
using fiskaltrust.Middleware.Queue.Bootstrapper;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using fiskaltrust.Middleware.Localization.QueueDE.Transactions;
using fiskaltrust.Middleware.Contracts;
using Moq;
using Castle.Core.Logging;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Contracts.Models;
using System.ServiceModel.Channels;

namespace fiskaltrust.Middleware.Localization.QueueDE.UnitTest
{
    public class QueueDEConfigurationTests
    {
        [Fact]
        public void QueueDEConfigurationFromMiddlewareConfiguration_EnableTarFileExportNull_ShouldReturnQueueDEConfigurationDefaults()
        {
            var middlewareConfiguration = new MiddlewareConfiguration
            {
                Configuration = new Dictionary<string, object>
                {
                }
            };

            var queueConfiguration = QueueDEConfiguration.FromMiddlewareConfiguration(Mock.Of<ILogger<QueueDEConfiguration>>(), middlewareConfiguration);

            queueConfiguration.TarFileExportMode.Should().Be(TarFileExportMode.All);
        }

        [Fact]
        public void QueueDEConfigurationFromMiddlewareConfiguration_EnableTarFileExportFalse_ShouldReturnQueueDEModeNone()
        {
            var middlewareConfiguration = new MiddlewareConfiguration
            {
                Configuration = new Dictionary<string, object>
                {
                    { "EnableTarFileExport", "false" }
                }
            };

            var queueConfiguration = QueueDEConfiguration.FromMiddlewareConfiguration(Mock.Of<ILogger<QueueDEConfiguration>>(), middlewareConfiguration);

            queueConfiguration.TarFileExportMode.Should().Be(TarFileExportMode.None);
        }

        [Fact]
        public void QueueDEConfigurationFromMiddlewareConfiguration_EnableTarFileExportTrue_ShouldReturnQueueDEModeAll ()
        {
            var middlewareConfiguration = new MiddlewareConfiguration
            {
                Configuration = new Dictionary<string, object>
                {
                    { "EnableTarFileExport", "true" }
                }
            };

            var queueConfiguration = QueueDEConfiguration.FromMiddlewareConfiguration(Mock.Of<ILogger<QueueDEConfiguration>>(), middlewareConfiguration);

            queueConfiguration.TarFileExportMode.Should().Be(TarFileExportMode.All);
        }

        [Fact]
        public void QueueDEConfigurationFromMiddlewareConfiguration_EnableTarFileExportAndTarFileExportMode_ShouldReturnQueueDEModeTarFileExportMode()
        {
            var middlewareConfiguration = new MiddlewareConfiguration
            {
                Configuration = new Dictionary<string, object>
                {
                    { "EnableTarFileExport", "true"},
                    { nameof(QueueDEConfiguration.TarFileExportMode), TarFileExportMode.Erased.ToString() },
                }
            };

            var logger = Mock.Of<ILogger<QueueDEConfiguration>>();
            Mock.Get(logger).Setup(logger => logger.Log(
                It.Is<LogLevel>(logLevel => logLevel == LogLevel.Warning),
                It.Is<EventId>(eventId => eventId.Id == 0),
                It.Is<It.IsAnyType>((@object, @type) => @object.ToString().Contains("TarFileExportMode = Erased") && @type.Name == "FormattedLogValues"),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()));

            var queueConfiguration = QueueDEConfiguration.FromMiddlewareConfiguration(Mock.Of<ILogger<QueueDEConfiguration>>(), middlewareConfiguration);

            queueConfiguration.TarFileExportMode.Should().Be(TarFileExportMode.Erased);
            Mock.Get(logger).Verify();
        }
    }
}

