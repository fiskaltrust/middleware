using System.Collections.Generic;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.v2;

public record ProcessCommandResponse(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals);
