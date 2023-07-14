﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Contracts.RequestCommands;
using System.Linq;
using fiskaltrust.Middleware.Contracts.Extensions;
using Newtonsoft.Json;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Contracts.Constants;
using System.Globalization;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Localization.QueueES.Factories;
using fiskaltrust.Middleware.Localization.QueueES.Services;
using fiskaltrust.Middleware.Localization.QueueES.Externals.ifpos;

#pragma warning disable

namespace fiskaltrust.Middleware.Localization.QueueES.RequestCommands
{
    public class PosReceiptCommand : RequestCommand
    {
        private readonly long _countryBaseState;
        private readonly ICountrySpecificQueueRepository _countrySpecificQueueRepository;
        private readonly ICountrySpecificSettings _countryspecificSettings;
        private readonly IConfigurationRepository _configurationRepository;
        private readonly SignatureItemFactoryES _signatureItemFactory;
        private readonly IMiddlewareJournalITRepository _journalITRepository;
        private readonly IESSSCD _client;
        private readonly ISSCD _signingDevice;
        private readonly ILogger<DailyClosingReceiptCommand> _logger;

        public PosReceiptCommand(ISSCD signingDevice, ILogger<DailyClosingReceiptCommand> logger, IESSSCDProvider sscdProvider, SignatureItemFactoryES signatureItemFactory, IMiddlewareJournalITRepository journalITRepository, IConfigurationRepository configurationRepository, ICountrySpecificSettings countrySpecificSettings)
        {
            _client = sscdProvider.Instance;
            _signatureItemFactory = signatureItemFactory;
            _journalITRepository = journalITRepository;
            _countryspecificSettings = countrySpecificSettings;
            _countrySpecificQueueRepository = countrySpecificSettings.CountrySpecificQueueRepository;
            _countryBaseState = countrySpecificSettings.CountryBaseState;
            _configurationRepository = configurationRepository;
            _signingDevice = signingDevice;
            _logger = logger;
        }

        public override async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, bool isBeingResent = false) => throw new NotImplementedException();

        public override Task<bool> ReceiptNeedsReprocessing(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem) => throw new NotImplementedException();
    }
}
