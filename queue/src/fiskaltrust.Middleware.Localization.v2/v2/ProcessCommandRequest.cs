using System.Runtime.CompilerServices;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.v2;

// Maybe add the scu state string here and in the response and pull the loading and saving of it into the v2.signprocessor.
public record ProcessCommandRequest(ftQueue queue, ReceiptRequest ReceiptRequest, ReceiptResponse ReceiptResponse);
