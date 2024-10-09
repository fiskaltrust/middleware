using System.Runtime.CompilerServices;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.v2.v2;

public record ProcessCommandRequest(ftQueue Queue, ReceiptRequest ReceiptRequest, ReceiptResponse ReceiptResponse, ftQueueItem QueueItem);
