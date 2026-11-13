namespace fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;

/// <summary>
/// The JET codes (R19 v4.2) an SCU signs for an entry the R13/R12 dictionaries do not cover (ADR 0021): the R19 code
/// where its meaning fits exactly, else a fiskaltrust code from 900 up, the range R19 leaves to the publisher
/// ("Fonction éditeurs"). The labels go in the archive README.
/// </summary>
public static class FRJetCodes
{
    /// <summary>0x4001 (Queue-start receipt): R19 260.</summary>
    public const int QueueStart = 260;
    public const string QueueStartLabel = "Initialisation des données";

    /// <summary>0x2000 (Zero receipt).</summary>
    public const int ZeroReceipt = 900;
    public const string ZeroReceiptLabel = "Reçu zéro";

    /// <summary>0x2001 ((Reserved) One receipt).</summary>
    public const int OneReceipt = 901;
    public const string OneReceiptLabel = "Reçu un";

    /// <summary>0x4021 (Initiate migration).</summary>
    public const int InitiateMigration = 910;
    public const string InitiateMigrationLabel = "Début de migration v1.2 vers v2";

    /// <summary>0x4022 (Finish migration).</summary>
    public const int FinishMigration = 911;
    public const string FinishMigrationLabel = "Fin de migration v1.2 vers v2";

    /// <summary>Training 0x3000 (Protocol, unspecified type).</summary>
    public const int TrainingUnspecifiedProtocol = 920;
    public const string TrainingUnspecifiedProtocolLabel = "Protocole non spécifié en formation";

    /// <summary>Training 0x3003 (Internal usage / material consumption).</summary>
    public const int TrainingInternalUsage = 921;
    public const string TrainingInternalUsageLabel = "Consommation interne en formation";

    /// <summary>The R19 codes a POS may send on 0x3002 (Protocol, audit / accounting event), besides the publisher codes.</summary>
    public static readonly IReadOnlyCollection<int> AccountingEventCodes = [160, 180];
}
