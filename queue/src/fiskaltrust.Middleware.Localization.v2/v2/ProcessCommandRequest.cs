using System.Runtime.CompilerServices;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.v2;

public record ProcessCommandRequest(ftQueue queue, ReceiptRequest ReceiptRequest, ReceiptResponse ReceiptResponse);
