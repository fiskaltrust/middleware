using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Localization.QueueME.Models;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueME.RequestCommands
{
    public abstract class RequestCommand
    {

        protected readonly ILogger<RequestCommand> _logger;
        protected readonly SignatureFactoryME _signatureFactory;

        public RequestCommand(ILogger<RequestCommand> logger, SignatureFactoryME signatureFactory)
        {
            _logger = logger;
            _signatureFactory = signatureFactory;
        }

        public abstract Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem);

    }
}