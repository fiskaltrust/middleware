using System.Globalization;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Factories;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;

/// <summary>
/// The JET events (Infocert R19 v4.2) the middleware asks the POS to send (ADR 0009): it never issues
/// a JET entry itself, it adds a JET item to a response and the POS sends the event as a
/// <c>ProtocolTechnicalEvent 0x3001</c>. One behaviour and one naming for every certification body
/// (decided 2026-11-07, market-fr#200). Labels are the R19 v4.2 "Intitulé" column.
/// </summary>
public static class FRJetItems
{
    public const int PeriodArchive = 20;
    public const string PeriodArchiveLabel = "Archivage fiscal de période";

    public const int PeriodClosing = 50;
    public const string PeriodClosingLabel = "Clôture de période (Jour/Mois)";

    public const int YearlyClosing = 60;
    public const string YearlyClosingLabel = "Clôture annuelle ou d'exercice";

    public const int DegradedModeStart = 70;
    public const string DegradedModeStartLabel = "Début de mode dégradé";

    public const int PointOfSaleRemoved = 127;
    public const string PointOfSaleRemovedLabel = "Suppression d'un point de vente ou d'un poste utilisateur";

    public const int NoteCopy = 155;
    public const string NoteCopyLabel = "Duplicata d'une note";

    public const int PaymentProofCopy = 156;
    public const string PaymentProofCopyLabel = "Duplicata d'un justificatif de paiement";

    public const int MajorVersionInstalled = 250;
    public const string MajorVersionInstalledLabel = "Installation d'une nouvelle version majeure du logiciel";

    public const int MinorVersionInstalled = 255;
    public const string MinorVersionInstalledLabel = "Installation d'une nouvelle version mineure du logiciel";

    public const int DataInitialisation = 260;
    public const string DataInitialisationLabel = "Initialisation des données";

    public const int ComplianceSettingChange = 270;
    public const string ComplianceSettingChangeLabel = "Evolution de paramètre de conformité aux règles";

    public const int SigningKeyChange = 450;
    public const string SigningKeyChangeLabel = "Traçabilité du changement du trousseau de clés utilisé pour signer";

    /// <summary>
    /// The items a signed, non-training receipt carries because of its own receipt case: the
    /// lifecycle receipts (0x4001 → 260, 0x4002 → 127, decided 2026-11-04) and the closings
    /// (daily and monthly → 50, yearly → 60). The shift closing is not a period of R19's code 50
    /// ("Jour/Mois").
    /// </summary>
    public static void AddForSignedReceipt(ReceiptRequest request, ReceiptResponse response)
    {
        if (request.ftReceiptCase.IsFlag(ReceiptCaseFlags.Training))
        {
            return;
        }

        var identification = Information(
            ("cbReceiptReference", request.cbReceiptReference),
            ("ftReceiptIdentification", response.ftReceiptIdentification));

        switch (request.ftReceiptCase.Case())
        {
            // TODO pending A2: the archive contract (section 6) and ADR 0009 list queue start as JET 80.
            case ReceiptCase.InitialOperationReceipt0x4001:
                response.ftSignatures.Add(SignaturItemFactory.CreateJetEventSignature(DataInitialisation, DataInitialisationLabel, identification));
                break;
            // TODO pending A2: the archive contract (section 6) and ADR 0009 list queue stop as JET 40.
            case ReceiptCase.OutOfOperationReceipt0x4002:
                response.ftSignatures.Add(SignaturItemFactory.CreateJetEventSignature(PointOfSaleRemoved, PointOfSaleRemovedLabel, identification));
                break;
            case ReceiptCase.DailyClosing0x2011:
            case ReceiptCase.MonthlyClosing0x2012:
                response.ftSignatures.Add(SignaturItemFactory.CreateJetEventSignature(PeriodClosing, PeriodClosingLabel, identification));
                break;
            case ReceiptCase.YearlyClosing0x2013:
                response.ftSignatures.Add(SignaturItemFactory.CreateJetEventSignature(YearlyClosing, YearlyClosingLabel, identification));
                break;
            // Every archive receipt the POS sends asks for JET 20; only a forced archive raises 30 (ADR 0018).
            case FRReceiptChainExt.ArchiveReceipt:
                response.ftSignatures.Add(SignaturItemFactory.CreateJetEventSignature(PeriodArchive, PeriodArchiveLabel, identification));
                break;
        }
    }

    /// <summary>
    /// The item a signed copy carries: 155 for a copy of a bill (<c>0x3004</c>, the "note"), 156 for
    /// a copy of a payment proof (<c>0x3005</c>). A copy of any other receipt has no R19 code.
    /// </summary>
    public static void AddForCopy(ReceiptRequest request, ReceiptResponse response, string copiedReceiptReference, ReceiptCase? copiedCase)
    {
        if (request.ftReceiptCase.IsFlag(ReceiptCaseFlags.Training))
        {
            return;
        }

        var (code, label) = copiedCase switch
        {
            // TODO pending A2: the archive contract (section 6) raises 155 on a copy of any bill (B) original, not only of 0x3004.
            ReceiptCase.Order0x3004 => (NoteCopy, NoteCopyLabel),
            ReceiptCase.Pay0x3005 => (PaymentProofCopy, PaymentProofCopyLabel),
            _ => (0, ""),
        };
        if (code == 0)
        {
            return;
        }

        response.ftSignatures.Add(SignaturItemFactory.CreateJetEventSignature(code, label, Information(
            ("copied.cbReceiptReference", copiedReceiptReference),
            ("cbReceiptReference", request.cbReceiptReference),
            ("ftReceiptIdentification", response.ftReceiptIdentification))));
    }

    /// <summary>A moment as JET-INF prints it.</summary>
    public static string Moment(DateTime? moment) => moment?.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK", CultureInfo.InvariantCulture) ?? "";

    /// <summary>The JET-INF text: <c>key=value</c> pairs separated by "; ", empty values left out.</summary>
    public static string Information(params (string key, string? value)[] pairs)
        => string.Join("; ", pairs.Where(x => !string.IsNullOrEmpty(x.value)).Select(x => $"{x.key}={x.value}"));
}
