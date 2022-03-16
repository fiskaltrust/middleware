using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueME
{
    public class JournalProcessorME : IJournalProcessor
    {
        private readonly ILogger<JournalProcessorME> _logger;

        public JournalProcessorME(
            ILogger<JournalProcessorME> logger)
        {
            _logger = logger;
        }

        public IAsyncEnumerable<JournalResponse> ProcessAsync(JournalRequest request)
        {
            throw new NotImplementedException();
        }

        private IAsyncEnumerable<JournalResponse> ProcessTarExportFromDatabaseAsync(JournalRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
