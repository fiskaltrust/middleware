using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using fiskaltrust.Middleware.Journal.FR.Archive.Csv;
using fiskaltrust.Middleware.Journal.FR.Archive.Model;
using fiskaltrust.Middleware.Journal.FR.Archive.Packaging;
using fiskaltrust.Middleware.Journal.FR.Archive.Signing;
using fiskaltrust.Middleware.Journal.FR.Archive.Writing;

namespace fiskaltrust.Middleware.Journal.FR.Archive.V2
{
    /// <summary>What kind of entries a journal holds, which decides its columns beyond the common ones.</summary>
    public enum ArchiveJournalKind
    {
        /// <summary>T, P, I, B, X: the receipt's own totals (archive-v2 §1.2).</summary>
        Sales,

        /// <summary>S, Z, M, Y and E: the period and perpetual totals of a closing (archive-v2 §1.3).</summary>
        Closing,

        /// <summary>D: what the copy copies, its number and its reason (archive-v2 §1.2 <i>Copy chain</i>).</summary>
        Copy,

        /// <summary>A: the boundaries the archive receipt signed (archive-v2 §1.4), no closing totals.</summary>
        Archive,

        /// <summary>L, D: the common columns only.</summary>
        Other,
    }

    /// <summary>
    /// One journal of the archive (archive-v2 field contract §7, market-fr#173): the journal code a POS
    /// asks for, the file it lands in, and the chain letters whose entries it holds.
    /// </summary>
    public sealed class ArchiveJournal
    {
        internal ArchiveJournal(int code, string letter, string name, string description, string frenchName, ArchiveJournalKind kind, params string[] chainLetters)
        {
            Code = code;
            Letter = letter;
            Name = name;
            Description = description;
            FrenchName = frenchName;
            Kind = kind;
            ChainLetters = new HashSet<string>(chainLetters, StringComparer.Ordinal);
        }

        /// <summary>The <c>jjj</c> part of the <c>ftJournalType</c>, e.g. 0x001 for tickets.</summary>
        public int Code { get; }

        /// <summary>The contract's letter, which leads the file name.</summary>
        public string Letter { get; }

        /// <summary>The English name in the file name, lower case with hyphens, e.g. <c>daily-closing</c>.</summary>
        public string Name { get; }

        public string Description { get; }

        /// <summary>The French name the archive documentation shows next to the letter.</summary>
        public string FrenchName { get; }

        public ArchiveJournalKind Kind { get; }

        /// <summary>
        /// The letters the queue writes into <c>ftReceiptIdentification</c> for this journal: the journal's own
        /// <see cref="Letter"/>, since every ADR 0002 chain has its own journal.
        /// </summary>
        public IReadOnlyCollection<string> ChainLetters { get; }

        /// <summary>
        /// <c>journal-&lt;letter&gt;-&lt;name&gt;.csv</c> (decided 2026-11-06): still one file per chain letter, and
        /// readable in a plain zip listing.
        /// </summary>
        public string FileName => $"journal-{Letter}-{Name}.csv";

        public bool Holds(ArchiveEntry entry) => ChainLetters.Contains(entry.Chain);
    }

    /// <summary>A French compliance parameter joined to the archive (Ref LNE 1.8 Exigence 19, archive-v2 §12).</summary>
    public sealed class ComplianceParameter
    {
        public ComplianceParameter(string component, string parameter, string? value)
        {
            Component = component;
            Parameter = parameter;
            Value = value;
        }

        /// <summary><c>Middleware</c>, <c>Queue</c> or <c>SCU</c>.</summary>
        public string Component { get; }

        public string Parameter { get; }

        public string? Value { get; }
    }

    /// <summary>What the export knows beyond the entries: the compliance parameters in force.</summary>
    public sealed class ArchiveV2Context
    {
        public IReadOnlyList<ComplianceParameter> ComplianceParameters { get; set; } = Array.Empty<ComplianceParameter>();
    }

    /// <summary>
    /// The files of a v2 archive file (ADR 0006, ADR 0019, archive-v2 field contract, market-fr#227, #221).
    /// </summary>
    public static class ArchiveV2Files
    {
        public static readonly IReadOnlyList<ArchiveJournal> Journals = new[]
        {
            new ArchiveJournal(0x001, "T", "ticket", "Ticket", "Tickets", ArchiveJournalKind.Sales, "T"),
            new ArchiveJournal(0x002, "P", "payment-proof", "Payment proof", "Justificatifs de paiement", ArchiveJournalKind.Sales, "P"),
            new ArchiveJournal(0x003, "I", "invoice", "Invoice", "Factures", ArchiveJournalKind.Sales, "I"),
            new ArchiveJournal(0x004, "E", "technical", "Technical", "Opérations techniques", ArchiveJournalKind.Closing, "E"),
            new ArchiveJournal(0x007, "B", "bill", "Bill (note)", "Notes", ArchiveJournalKind.Sales, "B"),
            new ArchiveJournal(0x008, "A", "archive", "Archive", "Archives", ArchiveJournalKind.Archive, "A"),
            new ArchiveJournal(0x009, "L", "log", "Log", "Journal des événements techniques (JET)", ArchiveJournalKind.Other, "L"),
            new ArchiveJournal(0x00A, "D", "copy", "Copy", "Duplicatas", ArchiveJournalKind.Copy, "D"),
            new ArchiveJournal(0x00B, "X", "training", "Training", "Mode école", ArchiveJournalKind.Sales, "X"),
            new ArchiveJournal(0x00C, "S", "shift-closing", "Shift closing", "Clôtures de service", ArchiveJournalKind.Closing, "S"),
            new ArchiveJournal(0x00D, "Z", "daily-closing", "Daily closing", "Clôtures journalières", ArchiveJournalKind.Closing, "Z"),
            new ArchiveJournal(0x00E, "M", "monthly-closing", "Monthly closing", "Clôtures mensuelles", ArchiveJournalKind.Closing, "M"),
            new ArchiveJournal(0x00F, "Y", "yearly-closing", "Yearly closing", "Clôtures annuelles", ArchiveJournalKind.Closing, "Y"),
        };

        /// <summary>
        /// One anchor per journal file with rows in <paramref name="file"/> (market-fr#262, contract section 10.2):
        /// <c>chain</c> is the journal letter, <c>firstReceiptIdentification</c> its first row, and <c>previousSignature</c>
        /// exactly what that row's <c>PreviousSignature</c> column holds (the Base64Url signature part, empty at a chain's start).
        /// </summary>
        public static IReadOnlyList<ManifestChainAnchor> ChainAnchorsOf(ArchiveFile<ArchiveEntry> file)
            => Journals
                .Select(journal => (journal, first: file.Records.FirstOrDefault(journal.Holds)))
                .Where(x => x.first is not null)
                .Select(x => new ManifestChainAnchor { Chain = x.journal.Letter, FirstReceiptIdentification = x.first!.ReceiptIdentification, PreviousSignature = x.first.PreviousSignature ?? "" })
                .ToList();

        /// <summary>
        /// The schema of <paramref name="file"/>: <c>LNE</c> when its receipts sign a delimited data set (LNE and
        /// self-certified SCUs), <c>InfoCert</c> when they sign a JWS. Both write the same files and columns (decided
        /// 2026-11-06); the schema says how to read <c>Signature</c>.
        /// </summary>
        public static string SchemaOf(ArchiveFile<ArchiveEntry> file)
            => file.FirstRecord.Signature.Contains('|') ? "LNE" : "InfoCert";

        /// <summary>An entry as the manifest names it (<c>firstRecord</c>, <c>lastRecord</c>).</summary>
        public static ManifestRecord Describe(ArchiveEntry entry)
            => new ManifestRecord { ReceiptIdentification = entry.ReceiptIdentification, QueueItemId = entry.QueueItemId, ReceiptMoment = entry.ReceiptMoment };

        /// <summary>
        /// The packager of v2 archive files: every manifest carries <c>schema</c>, <c>chainAnchors</c> and its first and last
        /// record, whoever signs (contract section 10.2).
        /// </summary>
        /// <param name="signerFor">The signer of a file: the SCU whose certificate signed its entries; throws when its key is not available.</param>
        public static ArchivePackager<ArchiveEntry> CreatePackager(Func<ArchiveFileKey, IArchiveSigner> signerFor, ArchiveV2Context? context = null, CsvOptions? options = null)
            => new ArchivePackager<ArchiveEntry>(new[] { CreateWriter(options, context) }, signerFor, Describe, ChainAnchorsOf, SchemaOf);

        public const string ChargeItemsFileName = "charge-items.csv";
        public const string PayItemsFileName = "pay-items.csv";
        public const string VatSummaryFileName = "receipt-vat-summary.csv";
        public const string SignaturesFileName = "signatures.csv";
        public const string JetFileName = "jet.csv";
        public const string CaseDataFileName = "case-data.csv";
        public const string ComplianceParametersFileName = "compliance-parameters.csv";

        private const long PayItemDownpaymentFlag = 0x0008_0000;
        private const long ChargeItemExtraOrDiscountFlag = 0x0004_0000;
        private const long ChargeItemDownpaymentFlag = 0x0008_0000;
        private const long ChargeItemReturnableDepositFlag = 0x0010_0000;
        private const long JetEventCase = 0x3001;

        private static readonly HashSet<string> ChargeItemChains = new HashSet<string>(new[] { "T", "I", "B", "X" }, StringComparer.Ordinal);
        private static readonly HashSet<string> PayItemChains = new HashSet<string>(new[] { "T", "P", "I", "B", "X" }, StringComparer.Ordinal);

        public static ArchiveJournal? JournalOf(int code) => Journals.FirstOrDefault(j => j.Code == code);

        /// <summary>The archive file an entry belongs to: its SCU certificate, format version 2 (ADR 0019).</summary>
        public static ArchiveFileKey KeyOf(ArchiveEntry entry)
            => new ArchiveFileKey(string.IsNullOrEmpty(entry.CertificateSerialNumber) ? "unknown" : entry.CertificateSerialNumber!, ArchiveFormatVersion.V2);

        /// <summary>The journal file of one journal, e.g. for a single-journal export.</summary>
        public static ICsvFileDefinition<ArchiveEntry> JournalFile(ArchiveJournal journal)
            => new FlatCsvFile<ArchiveEntry>(journal.FileName, JournalColumns(journal.Kind), journal.Holds,
                $"une ligne par document de la chaîne {journal.Letter} ({journal.FrenchName})");

        public static IReadOnlyList<CsvColumn<ArchiveEntry>> JournalColumns(ArchiveJournalKind kind) => kind switch
        {
            ArchiveJournalKind.Sales => CommonColumns.Concat(SalesColumns).Concat(SalesTotalsColumns).Concat(CustomerColumns).ToList(),
            ArchiveJournalKind.Closing => CommonColumns.Concat(ClosingPeriodColumns).Concat(ClosingTotalsColumns).ToList(),
            ArchiveJournalKind.Archive => CommonColumns.Concat(ArchiveReceiptColumns).ToList(),
            ArchiveJournalKind.Copy => CommonColumns.Concat(CopyColumns).ToList(),
            _ => CommonColumns,
        };

        /// <summary>The data files, in the order the archive lists them; <c>README.txt</c> comes last and describes them.</summary>
        public static IReadOnlyList<ICsvFileDefinition<ArchiveEntry>> Files(ArchiveV2Context? context = null)
        {
            context ??= new ArchiveV2Context();
            var files = Journals.Select(JournalFile)
                .Concat(new ICsvFileDefinition<ArchiveEntry>[]
                {
                    new OneToManyCsvFile<ArchiveEntry, JsonElement>(ChargeItemsFileName, e => ArchiveEntry.Array(e.Request, "cbChargeItems"), ChargeItemColumns, e => ChargeItemChains.Contains(e.Chain),
                        "une ligne par article (cbChargeItems) d'un document des chaînes T, I, B ou X"),
                    new OneToManyCsvFile<ArchiveEntry, JsonElement>(PayItemsFileName, e => ArchiveEntry.Array(e.Request, "cbPayItems"), PayItemColumns, e => PayItemChains.Contains(e.Chain),
                        "une ligne par paiement (cbPayItems) d'un document des chaînes T, P, I, B ou X"),
                    new OneToManyCsvFile<ArchiveEntry, KeyValuePair<string, JsonElement>>(VatSummaryFileName, VatSummaryOf, VatSummaryColumns, e => ChargeItemChains.Contains(e.Chain),
                        "une ligne par document et par taux de TVA (récapitulatif de TVA)"),
                    new OneToManyCsvFile<ArchiveEntry, JsonElement>(SignaturesFileName, e => ArchiveEntry.Array(e.Response, "ftSignatures"), SignatureColumns, null,
                        "une ligne par élément de signature (ftSignatures) de la réponse d'un document, toutes chaînes"),
                    new FlatCsvFile<ArchiveEntry>(JetFileName, JetColumns, IsJetEvent,
                        "une ligne par événement du journal des événements techniques (JET), sur la chaîne L"),
                    new OneToManyCsvFile<ArchiveEntry, CaseDataRow>(CaseDataFileName, CaseData.Rows, CaseDataColumns, null,
                        "une ligne par valeur élémentaire des données complémentaires (case data) et des champs structurés de la requête"),
                    new ComplianceParametersFile(context.ComplianceParameters),
                    new RequestsFile(),
                })
                .ToList();
            files.Add(new ArchiveReadmeFile(files.OfType<IDocumentedFile>().ToList()));
            return files;
        }

        public static IArchiveFileWriter<ArchiveEntry> CreateWriter(CsvOptions? options = null, ArchiveV2Context? context = null)
            => new CsvArchiveWriter<ArchiveEntry>(ArchiveFormatVersion.V2, Files(context), options);

        private static CsvColumn<ArchiveEntry> Col(string name, Func<ArchiveEntry, object?> value, string description) => new CsvColumn<ArchiveEntry>(name, value, description);

        private static readonly System.Text.RegularExpressions.Regex MajorMinorPatch = new System.Text.RegularExpressions.Regex("^[0-9]+\\.[0-9]+\\.[0-9]+", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        /// <summary>
        /// A version as <c>major.minor.patch</c> (archive-v2 §1.1, §12, #124): the leading three numbers of an informational
        /// version such as <c>1.3.93-ci.46+commit</c>. A version that does not start with them is kept as recorded.
        /// </summary>
        public static string? SoftwareVersion(string? version)
            => version is null ? null : MajorMinorPatch.Match(version) is { Success: true } match ? match.Value : version;

        private static CsvColumn<ChildRow<ArchiveEntry, T>> Child<T>(string name, Func<ChildRow<ArchiveEntry, T>, object?> value, string description)
            => new CsvColumn<ChildRow<ArchiveEntry, T>>(name, value, description);

        /// <summary>§1.1, the columns common to every chain.</summary>
        public static readonly IReadOnlyList<CsvColumn<ArchiveEntry>> CommonColumns = new[]
        {
            Col("ftQueueItemID", e => e.QueueItemId, "Identifiant unique du document (GUID). Clé de jointure avec tous les autres fichiers."),
            Col("ftQueueID", e => e.QueueId, "Identifiant de la file (queue) fiskaltrust qui a signé le document."),
            Col("ftCashBoxIdentification", e => ArchiveEntry.String(e.Response, "ftCashBoxIdentification"), "Identification de la caisse (terminal) fiskaltrust émettrice."),
            Col("ftQueueRow", e => e.QueueRow, "Numéro d'ordre de la requête dans la file, incrémental et sans trou."),
            Col("Chain", e => e.Chain, "Lettre de la chaîne de signature du document (liste 5.1)."),
            Col("ftReceiptIdentification", e => e.ReceiptIdentification, "Numéro du document : préfixe fiskaltrust, lettre de la chaîne et compteur de la chaîne."),
            Col("ChainCounter", e => e.ChainCounter, "Rang du document dans sa chaîne, signé ; continu et sans trou."),
            Col("StartOfChain", e => e.StartOfChain, "true pour le premier document d'une chaîne, qui n'a pas de signature précédente."),
            Col("PreviousSignature", e => e.PreviousSignature, "Signature (partie Base64Url) du document précédent de la même chaîne ; vide en début de chaîne. Assure le chaînage."),
            Col("ftReceiptMoment", e => e.ReceiptMoment, "Date et heure d'enregistrement du document par fiskaltrust (UTC)."),
            Col("cbReceiptMoment", e => MomentOrText(e.Request, "cbReceiptMoment"), "Date et heure de la transaction telles que le système de caisse les a envoyées (UTC)."),
            Col("cbReceiptReference", e => ArchiveEntry.String(e.Request, "cbReceiptReference"), "Référence du document dans le système de caisse."),
            Col("cbPreviousReceiptReference", e => References(e.Request, "cbPreviousReceiptReference"), "Référence(s) du document d'origine (annulation, remboursement, duplicata, justificatif) ; plusieurs références séparées par une virgule."),
            Col("ftReceiptCase", e => CsvValueFormatter.Hex(e.ReceiptCase), "Code du type de document et de ses options (hexadécimal, liste 5.3)."),
            Col("cbTerminalID", e => ArchiveEntry.Text(e.Request, "cbTerminalID"), "Identifiant du terminal du système de caisse."),
            Col("cbUser", e => ArchiveEntry.Text(e.Request, "cbUser"), "Opérateur (utilisateur) ayant réalisé l'opération."),
            Col("cbArea", e => ArchiveEntry.Text(e.Request, "cbArea"), "Table ou zone de vente, telle qu'envoyée par le système de caisse."),
            Col("ftState", e => CsvValueFormatter.Hex(e.State), "État de la file au moment du document (hexadécimal), dont le mode de signature différée."),
            Col("RequestHash", e => e.RequestHash, "Empreinte signée de la requête canonique (version:SHA-256 Base64), recalculable depuis requests.jsonl ; vide si le module de signature n'en signe pas."),
            Col("Siret", e => e.Siret, "Numéro SIRET de l'émetteur (14 chiffres), signé."),
            Col("Rcs", e => e.Issuer.Rcs, "Immatriculation RCS de l'émetteur, signée telle que configurée ; vide si elle n'est pas configurée."),
            Col("Naf", e => e.Issuer.Naf, "Code NAF (APE) de l'émetteur, signé tel que configuré ; vide s'il n'est pas configuré."),
            Col("LegalForm", e => e.Issuer.LegalForm, "Forme juridique et capital social de l'émetteur, signés tels que configurés ; vide s'ils ne sont pas configurés."),
            Col("VatOption", e => e.Issuer.VatOption, "Option de TVA de l'émetteur : debits (sur les débits) ou receipts (sur les encaissements), signée ; vide si elle n'est pas configurée."),
            Col("ElectronicAddress", e => e.Issuer.ElectronicAddress, "Adresse électronique de facturation de l'émetteur (SIREN ou SIREN_XXX, schéma 0225), signée ; vide si elle n'est pas configurée."),
            Col("CertificateSerialNumber", e => e.CertificateSerialNumber, "Numéro de série du certificat qui a signé le document."),
            Col("SoftwareVersion", e => SoftwareVersion(e.ProcessingVersion), "Version du logiciel fiskaltrust qui a traité le document, sous la forme majeure.mineure.correctif."),
            Col("Training", e => e.Training, "true si le document est émis en mode école (formation) ; sans valeur fiscale."),
            Col("Signature", e => e.Signature, "Signature du document : jeton JWS (InfoCert) ou données signées suivies de la signature après le dernier point (LNE)."),
        };

        private static readonly (string Name, string Label)[] TotalColumns =
        {
            ("Totalizer", "Total TTC"),
            ("CINormal", "Total TTC au taux normal de TVA"),
            ("CIDiscounted1", "Total TTC au taux réduit 1 de TVA"),
            ("CIDiscounted2", "Total TTC au taux réduit 2 de TVA"),
            ("CISuperReduced1", "Total TTC au taux super-réduit de TVA"),
            ("CIZero", "Total TTC au taux zéro ou exonéré"),
            ("CIUnknown", "Total TTC à un taux de TVA non classé"),
            ("PICash", "Total des paiements en espèces"),
            ("PINonCash", "Total des paiements autres qu'en espèces (carte, chèque, bon, paiement en ligne)"),
            ("PIInternal", "Total des mouvements internes (consommation interne, transferts)"),
            ("PIUnknown", "Total des paiements d'un type non classé"),
        };

        /// <summary>
        /// §1.2: what a sales document is, as the queue stored it when it signed (<c>ftStateData.FR</c>), and what the request
        /// carries for training and the guest count.
        /// </summary>
        public static readonly IReadOnlyList<CsvColumn<ArchiveEntry>> SalesColumns = new[]
        {
            Col("TotalHT", e => StateNumber(e.StateFR, "TotalHT"), "Total HT du document : somme des montants HT des articles tels que signés, déductions d'acomptes comprises ; égal au total HT du récapitulatif de TVA. Vide sur la chaîne P."),
            Col("PrintCount", e => StateNumber(e.StateFR, "PrintCount"), "Nombre d'impressions du document original : 1. Chaque réimpression est un duplicata de la chaîne D, numéroté dans CopyNumber."),
            Col("GuestCount", GuestCount, "Nombre de clients ou de couverts, tel qu'envoyé par le système de caisse (ftReceiptCaseData.FR.GuestCount) ; non signé, non contrôlé. Seulement sur les tickets et les notes ; vide s'il n'est pas envoyé."),
            Col("DocumentType", e => StateText(e, "DocumentType"), "Nature du document (liste 5.8) : ticket, payment-proof, invoice, receipt-without-obligation, delivery-note, note, pro-forma ou order ; un document en mode école a la nature du document qu'il simule."),
            Col("OperationType", e => StateText(e, "OperationType"), "Nature de l'opération : sale (vente), refund (remboursement) ou void (annulation), selon les options de ftReceiptCase."),
            Col("TrainingMention", TrainingMention, "Sur la chaîne X : la mention imprimée du mode école."),
            Col("ManagerId", e => CaseDataFR(e.Request, "ftReceiptCaseData", "ManagerId"), "Sur la chaîne X : identifiant du responsable de l'opérateur en mode école."),
            Col("DiscountTotal", e => StateNumber(e.StateFR, "DiscountTotal"), "Total TTC des remises et suppléments du document, sur ligne et sur le document."),
            Col("LineCount", e => StateNumber(e.StateFR, "LineCount"), "Nombre de lignes de vente du document, tel que signé (hors remises, acomptes et consignes)."),
        };

        /// <summary>§1.2 <i>Customer</i> (D8): the request's <c>cbCustomer</c>, and the endpoint the queue inferred for a French business buyer.</summary>
        public static readonly IReadOnlyList<CsvColumn<ArchiveEntry>> CustomerColumns = new[]
        {
            Col("CustomerName", e => Customer(e, "CustomerName"), "Nom ou raison sociale du client."),
            Col("CustomerId", e => Customer(e, "CustomerId"), "Numéro du client (numéro de compte client sur un ticket)."),
            Col("CustomerStreet", e => Customer(e, "CustomerStreet"), "Adresse du client : rue."),
            Col("CustomerZip", e => Customer(e, "CustomerZip"), "Adresse du client : code postal."),
            Col("CustomerCity", e => Customer(e, "CustomerCity"), "Adresse du client : ville."),
            Col("CustomerCountrySubentity", e => Customer(e, "CustomerCountrySubentity"), "Adresse du client : subdivision du pays (région, état)."),
            Col("CustomerCountry", e => Customer(e, "CustomerCountry"), "Pays du client (code ISO 3166-1 alpha-2)."),
            Col("CustomerVATId", e => Customer(e, "CustomerVATId"), "Numéro de TVA intracommunautaire du client professionnel ; vide pour un particulier."),
            Col("CustomerTaxId", e => Customer(e, "CustomerTaxId"), "Identifiant fiscal du client ; pour une entreprise française, son SIREN (9 chiffres) ou son SIRET (14 chiffres)."),
            Col("CustomerEndpointId", e => Customer(e, "CustomerEndpointId") ?? StateText(e, "CustomerEndpointId"), "Adresse électronique du client (schéma:identifiant) ; pour une entreprise française qui n'en envoie pas, 0225:<SIREN> déduit à la signature."),
            Col("CustomerReference", e => Customer(e, "CustomerReference"), "Référence de l'acheteur (BT-10)."),
        };

        /// <summary>
        /// §1.2 <i>Copy chain (D)</i>: the POS reference of the copied receipt, its fiscal number and type as the queue found
        /// them when it signed the copy, the copy's number and its reason. None of them is signed but the number.
        /// </summary>
        public static readonly IReadOnlyList<CsvColumn<ArchiveEntry>> CopyColumns = new[]
        {
            Col("CopiedReceiptReference", e => References(e.Request, "cbPreviousReceiptReference"), "Référence, dans le système de caisse, du document dont c'est le duplicata."),
            Col("CopiedReceiptIdentification", e => StateText(e, "CopiedReceiptIdentification"), "Numéro fiscal (ftReceiptIdentification) du document dupliqué ; vide s'il n'est pas enregistré dans cette caisse."),
            Col("CopyType", e => StateText(e, "CopyType"), "Nature du document dupliqué (liste 5.8) ; vide s'il n'est pas enregistré dans cette caisse."),
            Col("CopyNumber", e => StateNumber(e.StateFR, "CopyNumber"), "Rang du duplicata pour ce document : 1 pour le premier. Signé."),
            Col("CopyReason", e => CaseDataFR(e.Request, "ftReceiptCaseData", "CopyReason"), "Motif du duplicata, saisi dans le système de caisse."),
        };

        /// <summary>§1.2: the receipt's own totals, stored by the queue in <c>ftStateData.FR.Totals</c>.</summary>
        public static readonly IReadOnlyList<CsvColumn<ArchiveEntry>> SalesTotalsColumns = TotalColumns
            .Select(t => Col(t.Name, e => StateNumber(e.StateFR, "Totals", t.Name), t.Label + " du document, acomptes exclus."))
            .ToList();

        /// <summary>
        /// §1.3: which period a closing or technical receipt closes, stored by the queue in <c>ftStateData.FR.Closing</c>.
        /// Not signed. <c>PeriodStart</c> and <c>PeriodEnd</c> are empty on a receipt that closes no period.
        /// </summary>
        public static readonly IReadOnlyList<CsvColumn<ArchiveEntry>> ClosingPeriodColumns = new[]
        {
            Col("ClosingType", e => ClosingText(e, "ClosingType"), "Type de clôture : zero (reçu zéro), shift (service), daily (jour), monthly (mois), yearly (année ou exercice), lifecycle (mise en service, mise hors service et autres opérations techniques). Avec PeriodStart et PeriodEnd, identifie la période close."),
            Col("PeriodStart", e => ClosingMoment(e, "PeriodStart"), "Début (UTC) de la période close : la clôture précédente de même période, ou la mise en service de la caisse. Non signé ; vide si le document ne clôt aucune période."),
            Col("PeriodEnd", e => ClosingMoment(e, "PeriodEnd"), "Fin (UTC) de la période close : le moment du document de clôture. Signée (champ 12 du jeu de données LNE) ; vide si le document ne clôt aucune période."),
        };

        /// <summary>§1.3: the period totals of a closing, then the perpetual totals, stored in <c>ftStateData.FR.Totals</c>.</summary>
        public static readonly IReadOnlyList<CsvColumn<ArchiveEntry>> ClosingTotalsColumns = TotalColumns
            .Select(t => Col(t.Name, e => StateNumber(e.StateFR, "Totals", t.Name), t.Label + " de la période close (grand total de période)."))
            .Concat(TotalColumns.Select(t => Col("Perpetual" + t.Name, e => StateNumber(e.StateFR, "Totals", "Perpetual", t.Name), t.Label + " perpétuel, depuis le début de la chaîne (grand total perpétuel).")))
            .ToList();

        /// <summary>
        /// §1.4: the boundaries of the archive an archive receipt (0x3011) closes, as the queue handed them to the SCU
        /// in <c>ftStateData.FR.Archive</c> and the SCU signed them, and the archive-period total. The archive-period
        /// totals per VAT case and pay-item class are not archived.
        /// </summary>
        public static readonly IReadOnlyList<CsvColumn<ArchiveEntry>> ArchiveReceiptColumns = new[]
        {
            Col("PreviousArchiveQueueItemID", e => ArchiveId(e, "PreviousArchiveQueueItemId"), "Identifiant (ftQueueItemID) du reçu d'archive précédent ; vide pour le premier reçu d'archive de la caisse. Chaîne les archives."),
            Col("FirstContainedReceiptQueueItemID", e => ArchiveId(e, "FirstContainedReceiptQueueItemId"), "Identifiant du premier enregistrement contenu dans l'archive, signé : l'enregistrement qui suit le reçu d'archive précédent."),
            Col("FirstContainedReceiptMoment", e => ArchiveMoment(e, "FirstContainedReceiptMoment"), "Date et heure (UTC) du premier enregistrement contenu dans l'archive, signées."),
            Col("LastContainedReceiptQueueItemID", e => ArchiveId(e, "LastContainedReceiptQueueItemId"), "Identifiant du dernier enregistrement contenu dans l'archive, signé : le reçu d'archive lui-même."),
            Col("LastContainedReceiptMoment", e => ArchiveMoment(e, "LastContainedReceiptMoment"), "Date et heure (UTC) du dernier enregistrement contenu dans l'archive, signées."),
            Col("LastReceiptJournalID", e => ArchiveId(e, "LastReceiptJournalId"), "Dernière ligne du journal des reçus avant le reçu d'archive, signée."),
            Col("LastActionJournalID", e => ArchiveId(e, "LastActionJournalId"), "Dernière ligne du journal des actions avant le reçu d'archive, signée."),
            Col("LastJournalFRID", e => ArchiveId(e, "LastJournalFRId"), "Dernière ligne ftJournalFR avant le reçu d'archive, signée ; vide, la version 2 n'en écrit pas."),
            Col("ATotalizer", e => StateNumber(e.StateFR, "Totals", "Totalizer"), "Total TTC de la période d'archivage close par le reçu."),
        };

        /// <summary>§2, the charge items: the POS's values and the values the queue computed and stored.</summary>
        public static readonly IReadOnlyList<CsvColumn<ChildRow<ArchiveEntry, JsonElement>>> ChargeItemColumns = new[]
        {
            Child<JsonElement>("ftQueueItemID", r => r.Parent.QueueItemId, "Identifiant du document (jointure avec les journaux)."),
            Child<JsonElement>("ftReceiptIdentification", r => r.Parent.ReceiptIdentification, "Numéro du document, pour contrôler la jointure."),
            Child<JsonElement>("Position", r => Number(r.Child, "Position"), "Numéro de la ligne dans le document ; avec ftQueueItemID, identifie la ligne (jointure avec case-data.csv)."),
            Child<JsonElement>("ProductNumber", r => ArchiveEntry.Text(r.Child, "ProductNumber"), "Code de l'article."),
            Child<JsonElement>("Description", r => ArchiveEntry.Text(r.Child, "Description"), "Libellé de l'article."),
            Child<JsonElement>("Quantity", r => Number(r.Child, "Quantity"), "Quantité ; négative sur un remboursement."),
            Child<JsonElement>("Unit", r => ArchiveEntry.Text(r.Child, "Unit"), "Unité de la quantité."),
            Child<JsonElement>("UnitPrice", r => Number(r.Child, "UnitPrice"), "Prix unitaire TTC (TVA comprise)."),
            Child<JsonElement>("UnitPriceHT", r => LineNumber(r, "UnitPriceHT"), "Prix unitaire HT (hors TVA) : montant HT divisé par la quantité, arrondi à six décimales (un prix unitaire peut être inférieur au centime)."),
            Child<JsonElement>("Amount", r => Number(r.Child, "Amount"), "Montant total TTC (TVA comprise) de la ligne."),
            Child<JsonElement>("PricesIncludeVAT", r => true, "Toujours true : UnitPrice et Amount sont des montants TVA comprise."),
            Child<JsonElement>("AmountHT", r => LineNumber(r, "AmountHT"), "Montant total HT (hors TVA) de la ligne : Amount moins VATAmount."),
            Child<JsonElement>("VATRate", r => Number(r.Child, "VATRate"), "Taux de TVA en pourcentage (20, 5.5…)."),
            Child<JsonElement>("VATAmount", r => LineNumber(r, "VATAmount"), "Montant de TVA de la ligne : celui envoyé par la caisse, sinon calculé depuis le taux et arrondi au centime."),
            Child<JsonElement>("VATNature", r => LineText(r, "VATNature"), "Code à deux chiffres de la nature de TVA (exonération, autoliquidation…) ; 00 pour une TVA ordinaire."),
            Child<JsonElement>("ftChargeItemCase", r => Hex(r.Child, "ftChargeItemCase"), "Code du type de ligne : taux de TVA, type de prestation et options (hexadécimal, liste 5.3)."),
            Child<JsonElement>("Moment", r => MomentOrText(r.Child, "Moment"), "Date et heure de la ligne, si la caisse l'a envoyée (UTC)."),
            Child<JsonElement>("CountsAsLine", r => LineBool(r, "CountsAsLine"), "true si la ligne compte comme ligne de vente (LineCount), comme le module de signature la compte."),
            Child<JsonElement>("DiscountCode", r => LineText(r, "DiscountCode"), "Sur une ligne de vente : code(s) des remises qui s'y appliquent, envoyés par la caisse, séparés par une virgule."),
            Child<JsonElement>("DiscountRate", r => LineText(r, "DiscountRate"), "Sur une ligne de vente : taux des remises qui s'y appliquent, en pourcentage, séparés par une virgule."),
            Child<JsonElement>("DiscountAmount", r => LineNumber(r, "DiscountAmount"), "Sur une ligne de vente : total TTC des remises et suppléments qui s'y appliquent."),
            Child<JsonElement>("AppliesTo", r => LineNumber(r, "AppliesTo"), "Sur une remise ou un supplément : Position de la ligne à laquelle elle s'applique ; vide pour une remise sur le document."),
            Child<JsonElement>("Downpayment", r => Flag(r.Child, "ftChargeItemCase", ChargeItemDownpaymentFlag), "true si la ligne est un acompte ; exclue des totaux."),
            Child<JsonElement>("ExtraOrDiscount", r => Flag(r.Child, "ftChargeItemCase", ChargeItemExtraOrDiscountFlag), "true si la ligne est une remise ou un supplément."),
            Child<JsonElement>("ReturnableDeposit", r => Flag(r.Child, "ftChargeItemCase", ChargeItemReturnableDepositFlag), "true si la ligne est une consigne."),
        };

        /// <summary>§3, the pay-item columns.</summary>
        public static readonly IReadOnlyList<CsvColumn<ChildRow<ArchiveEntry, JsonElement>>> PayItemColumns = new[]
        {
            Child<JsonElement>("ftQueueItemID", r => r.Parent.QueueItemId, "Identifiant du document (jointure avec les journaux)."),
            Child<JsonElement>("ftReceiptIdentification", r => r.Parent.ReceiptIdentification, "Numéro du document, pour contrôler la jointure."),
            Child<JsonElement>("Position", r => Number(r.Child, "Position"), "Numéro du paiement dans le document."),
            Child<JsonElement>("ftPayItemCase", r => Hex(r.Child, "ftPayItemCase"), "Code du moyen de paiement et de ses options (hexadécimal, liste 5.3)."),
            Child<JsonElement>("Description", r => ArchiveEntry.Text(r.Child, "Description"), "Libellé du paiement."),
            Child<JsonElement>("Amount", r => Number(r.Child, "Amount"), "Montant total du paiement ; négatif sur un remboursement."),
            Child<JsonElement>("Quantity", r => Number(r.Child, "Quantity"), "Nombre de paiements de même valeur regroupés sur la ligne (par exemple plusieurs bons d'un même montant) ; Amount en est le total. Vaut 1 en pratique."),
            Child<JsonElement>("Moment", r => (object?) MomentOrText(r.Child, "Moment") ?? r.Parent.ReceiptMoment, "Date et heure du paiement (UTC) ; à défaut, celle du document."),
            Child<JsonElement>("Currency", r => CaseDataFR(r.Child, "ftPayItemCaseData", "Currency"), "Devise du paiement (ISO 4217), si ce n'est pas l'euro."),
            Child<JsonElement>("ExchangeRate", r => CaseDataFR(r.Child, "ftPayItemCaseData", "ExchangeRate"), "Taux de change appliqué."),
            Child<JsonElement>("Downpayment", r => Flag(r.Child, "ftPayItemCase", PayItemDownpaymentFlag), "true si le paiement est un acompte ; exclu des totaux."),
        };

        /// <summary>§4, the VAT summary per receipt and rate, stored by the queue in <c>ftStateData.FR.VATSummary</c>.</summary>
        public static readonly IReadOnlyList<CsvColumn<ChildRow<ArchiveEntry, KeyValuePair<string, JsonElement>>>> VatSummaryColumns = new[]
        {
            Child<KeyValuePair<string, JsonElement>>("ftQueueItemID", r => r.Parent.QueueItemId, "Identifiant du document (jointure avec les journaux)."),
            Child<KeyValuePair<string, JsonElement>>("ftReceiptIdentification", r => r.Parent.ReceiptIdentification, "Numéro du document, pour contrôler la jointure."),
            Child<KeyValuePair<string, JsonElement>>("VATRate", r => r.Child.Key, "Taux de TVA en pourcentage."),
            Child<KeyValuePair<string, JsonElement>>("VATNature", r => ArchiveEntry.Text(r.Child.Value, "VATNature"), "Code(s) de nature de TVA des lignes à ce taux, séparés par une virgule."),
            Child<KeyValuePair<string, JsonElement>>("AmountHT", r => RawNumber(r.Child.Value, "AmountHT"), "Base HT à ce taux."),
            Child<KeyValuePair<string, JsonElement>>("VATAmount", r => RawNumber(r.Child.Value, "VATAmount"), "Montant de TVA à ce taux : somme des TVA non arrondies des lignes, arrondie une fois au centime. Il peut différer d'un centime de la somme des VATAmount de charge-items.csv, arrondis ligne par ligne."),
            Child<KeyValuePair<string, JsonElement>>("AmountTTC", r => RawNumber(r.Child.Value, "AmountTTC"), "Total TTC à ce taux : somme des montants TTC des lignes, acomptes, remises et remboursements compris."),
        };

        /// <summary>§5, every signature item of a response.</summary>
        public static readonly IReadOnlyList<CsvColumn<ChildRow<ArchiveEntry, JsonElement>>> SignatureColumns = new[]
        {
            Child<JsonElement>("ftQueueItemID", r => r.Parent.QueueItemId, "Identifiant du document (jointure avec les journaux)."),
            Child<JsonElement>("ftReceiptIdentification", r => r.Parent.ReceiptIdentification, "Numéro du document, pour contrôler la jointure."),
            Child<JsonElement>("Index", r => r.Index, "Rang de l'élément dans la réponse, à partir de 0."),
            Child<JsonElement>("ftSignatureType", r => Hex(r.Child, "ftSignatureType"), "Type de l'élément (hexadécimal, liste 5.5)."),
            Child<JsonElement>("ftSignatureFormat", r => Hex(r.Child, "ftSignatureFormat"), "Format de l'élément : texte, QR code… (hexadécimal)."),
            Child<JsonElement>("Caption", r => ArchiveEntry.Text(r.Child, "Caption"), "Intitulé imprimé de l'élément."),
            Child<JsonElement>("Data", r => ArchiveEntry.Text(r.Child, "Data"), "Contenu de l'élément : QR code de signature ou mention imprimée."),
        };

        /// <summary>§6, one row per JET event (a non-training 0x3001 receipt on L).</summary>
        public static readonly IReadOnlyList<CsvColumn<ArchiveEntry>> JetColumns = new[]
        {
            Col("ftQueueItemID", e => e.QueueItemId, "Identifiant du document de l'événement (jointure avec le journal L)."),
            Col("ftReceiptIdentification", e => e.ReceiptIdentification, "Numéro du document de l'événement sur la chaîne L."),
            Col("EventCode", e => ReceiptCaseDataFR(e.Request, "Code"), "Code de l'événement (liste 5.6)."),
            Col("EventLabel", e => ReceiptCaseDataFR(e.Request, "Message"), "Libellé de l'événement."),
            Col("OperatorID", e => ArchiveEntry.Text(e.Request, "cbUser"), "Opérateur ayant déclenché l'événement."),
            Col("ftReceiptMoment", e => e.ReceiptMoment, "Date et heure de l'événement (UTC)."),
            Col("Information", e => ReceiptCaseDataFR(e.Request, "Information"), "Information complémentaire sur l'événement."),
            Col("ftCashBoxIdentification", e => ArchiveEntry.String(e.Response, "ftCashBoxIdentification"), "Identification de la caisse."),
            Col("PreviousSignature", e => e.PreviousSignature, "Signature de l'événement précédent de la chaîne L."),
            Col("Signature", e => e.Signature, "Signature de l'événement."),
        };

        /// <summary>§8, the last-resort copy of the case data and object-typed request fields.</summary>
        public static readonly IReadOnlyList<CsvColumn<ChildRow<ArchiveEntry, CaseDataRow>>> CaseDataColumns = new[]
        {
            Child<CaseDataRow>("ftQueueItemID", r => r.Parent.QueueItemId, "Identifiant du document (jointure avec les journaux)."),
            Child<CaseDataRow>("ftReceiptIdentification", r => r.Parent.ReceiptIdentification, "Numéro du document, pour contrôler la jointure."),
            Child<CaseDataRow>("Owner", r => r.Child.Owner, "Niveau de la donnée : Receipt (document), ChargeItem (ligne) ou PayItem (paiement)."),
            Child<CaseDataRow>("Position", r => r.Child.Position, "Position de la ligne ou du paiement ; vide au niveau du document."),
            Child<CaseDataRow>("Field", r => r.Child.Field, "Nom du champ de la requête, par exemple ftChargeItemCaseData ou cbCustomer."),
            Child<CaseDataRow>("Path", r => r.Child.Path, "Chemin de la valeur dans le champ (pointeur JSON, RFC 6901) ; vide pour la racine."),
            Child<CaseDataRow>("ValueType", r => r.Child.ValueType, "Type de la valeur : string, number, boolean, null, object ou array."),
            Child<CaseDataRow>("Value", r => r.Child.Value, "La valeur, en texte ; vide pour null et pour un objet ou un tableau."),
        };

        private static bool IsJetEvent(ArchiveEntry entry) => entry.Chain == "L" && !entry.Training && (entry.ReceiptCase & 0xFFFF) == JetEventCase;

        private static IEnumerable<KeyValuePair<string, JsonElement>> VatSummaryOf(ArchiveEntry entry)
            => entry.StateFR is { } fr && ArchiveEntry.Property(fr, "VATSummary") is { ValueKind: JsonValueKind.Object } summary
                ? summary.EnumerateObject().Select(p => new KeyValuePair<string, JsonElement>(p.Name, p.Value)).ToList()
                : Enumerable.Empty<KeyValuePair<string, JsonElement>>();

        /// <summary>The stored line values of a charge item: same index in <c>ftStateData.FR.ChargeItems</c> as in the request.</summary>
        private static JsonElement? Line(ChildRow<ArchiveEntry, JsonElement> row)
            => row.Parent.StateFR is { } fr && ArchiveEntry.Property(fr, "ChargeItems") is { ValueKind: JsonValueKind.Array } lines && row.Index < lines.GetArrayLength()
                ? lines[row.Index]
                : (JsonElement?) null;

        private static string? LineNumber(ChildRow<ArchiveEntry, JsonElement> row, string name) => Line(row) is { } line ? RawNumber(line, name) : null;

        private static string? LineText(ChildRow<ArchiveEntry, JsonElement> row, string name) => Line(row) is { } line ? ArchiveEntry.Text(line, name) : null;

        private static bool? LineBool(ChildRow<ArchiveEntry, JsonElement> row, string name)
            => Line(row) is { } line && ArchiveEntry.Property(line, name) is { } value && (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False)
                ? value.ValueKind == JsonValueKind.True
                : (bool?) null;

        /// <summary>
        /// <c>ftReceiptCaseData.FR.GuestCount</c> as sent, on the documents R13 gives a guest count (<c>ENC-TIK-CLI-NBR</c>):
        /// tickets and notes, a training one included (decided 2026-11-12, market-fr#221). Never signed, not validated.
        /// </summary>
        private static string? GuestCount(ArchiveEntry entry)
            => StateText(entry, "DocumentType") is "ticket" or "note" or "receipt-without-obligation" or "delivery-note" or "order"
                ? ReceiptCaseDataFR(entry.Request, "GuestCount")
                : null;

        private static string? StateText(ArchiveEntry entry, string name) => entry.StateFR is { } fr ? ArchiveEntry.Text(fr, name) : null;

        private static string? Customer(ArchiveEntry entry, string name)
            => ArchiveEntry.Property(entry.Request, "cbCustomer") is { ValueKind: JsonValueKind.Object } customer ? ArchiveEntry.Text(customer, name) : null;

        /// <summary>The training mention the queue printed (caption <c>Mode école</c>, contract §5).</summary>
        private static object? TrainingMention(ArchiveEntry entry)
            => ArchiveEntry.Array(entry.Response, "ftSignatures")
                .Where(item => ArchiveEntry.String(item, "Caption") == "Mode école")
                .Select(item => ArchiveEntry.Text(item, "Data"))
                .FirstOrDefault();

        private static JsonElement? ClosingBlock(ArchiveEntry entry)
            => entry.StateFR is { } fr && ArchiveEntry.Property(fr, "Closing") is { ValueKind: JsonValueKind.Object } closing ? closing : (JsonElement?) null;

        private static string? ClosingText(ArchiveEntry entry, string name) => ClosingBlock(entry) is { } closing ? ArchiveEntry.String(closing, name) : null;

        /// <summary>A moment of the closing period, in UTC; a value that is no date as stored.</summary>
        private static object? ClosingMoment(ArchiveEntry entry, string name)
            => ClosingBlock(entry) is { } closing
                ? ArchiveEntry.Moment(closing, name) is { } moment ? moment : (object?) ArchiveEntry.String(closing, name)
                : null;

        private static JsonElement? ArchiveBoundaries(ArchiveEntry entry)
            => entry.StateFR is { } fr && ArchiveEntry.Property(fr, "Archive") is { ValueKind: JsonValueKind.Object } archive ? archive : (JsonElement?) null;

        /// <summary>A GUID of the boundaries, written as "D"; a value that is no GUID as stored.</summary>
        private static object? ArchiveId(ArchiveEntry entry, string name)
            => ArchiveBoundaries(entry) is { } archive && ArchiveEntry.String(archive, name) is { } text
                ? Guid.TryParse(text, out var id) ? id : (object) text
                : null;

        /// <summary>A moment of the boundaries, in UTC; a value that is no date as stored.</summary>
        private static object? ArchiveMoment(ArchiveEntry entry, string name)
            => ArchiveBoundaries(entry) is { } archive
                ? ArchiveEntry.Moment(archive, name) is { } moment ? moment : (object?) ArchiveEntry.String(archive, name)
                : null;

        private static string? StateNumber(JsonElement? element, params string[] path)
        {
            if (element is not { } current)
            {
                return null;
            }
            for (var i = 0; i < path.Length - 1; i++)
            {
                if (ArchiveEntry.Property(current, path[i]) is not { ValueKind: JsonValueKind.Object } next)
                {
                    return null;
                }
                current = next;
            }
            return RawNumber(current, path[path.Length - 1]);
        }

        private static string? RawNumber(JsonElement element, string name)
            => ArchiveEntry.Property(element, name) is { ValueKind: JsonValueKind.Number } value ? Plain(value.GetRawText()) : null;

        /// <summary>
        /// A JSON number as stored, unless it has an exponent: then its exact value in plain decimals (archive-v2 Conventions
        /// <i>Decimals</i>), so no reader meets <c>1E-05</c>. A number beyond the decimal range is kept as stored.
        /// </summary>
        private static string Plain(string raw)
            => raw.IndexOfAny(new[] { 'e', 'E' }) >= 0 && decimal.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value)
                ? value.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : raw;

        private static bool? Flag(JsonElement element, string name, long flag)
            => ArchiveEntry.Long(element, name) is { } value ? (value & flag) != 0 : (bool?) null;

        /// <summary>A number as the POS sent it: the stored JSON text, so no rounding or reformatting.</summary>
        private static string? Number(JsonElement element, string name)
            => ArchiveEntry.Property(element, name) is { } value && value.ValueKind == JsonValueKind.Number ? Plain(value.GetRawText()) : ArchiveEntry.Text(element, name);

        private static string? Hex(JsonElement element, string name)
            => ArchiveEntry.Long(element, name) is { } value ? CsvValueFormatter.Hex(value) : null;

        private static object? MomentOrText(JsonElement element, string name)
            => (object?) ArchiveEntry.Moment(element, name) ?? ArchiveEntry.Text(element, name);

        /// <summary>One reference, or several joined by commas (v2 allows an array of previous references).</summary>
        private static string? References(JsonElement element, string name)
            => ArchiveEntry.Property(element, name) is { } value && value.ValueKind == JsonValueKind.Array
                ? string.Join(",", value.EnumerateArray().Select(v => v.ValueKind == JsonValueKind.String ? v.GetString() : v.GetRawText()))
                : ArchiveEntry.Text(element, name);

        /// <summary>A key of the FR country block of an item's case data (D2b).</summary>
        private static string? CaseDataFR(JsonElement item, string field, string key)
            => ArchiveEntry.Property(item, field) is { } caseData && ArchiveEntry.Property(caseData, "FR") is { } fr
                ? (ArchiveEntry.Property(fr, key) is { } value && value.ValueKind == JsonValueKind.Number ? value.GetRawText() : ArchiveEntry.Text(fr, key))
                : null;

        /// <summary>
        /// A key of <c>ftReceiptCaseData.FR</c>, read case-insensitively, the case data being an object or a JSON
        /// string (archive-v2 §6).
        /// </summary>
        private static string? ReceiptCaseDataFR(JsonElement request, string key)
        {
            var caseData = ArchiveEntry.Property(request, "ftReceiptCaseData");
            if (caseData is { ValueKind: JsonValueKind.String } text)
            {
                try
                {
                    using var document = JsonDocument.Parse(text.GetString()!);
                    caseData = document.RootElement.Clone();
                }
                catch (JsonException)
                {
                    return null;
                }
            }
            if (caseData is not { ValueKind: JsonValueKind.Object } data || PropertyIgnoreCase(data, "FR") is not { ValueKind: JsonValueKind.Object } fr)
            {
                return null;
            }
            return PropertyIgnoreCase(fr, key) is { } value
                ? value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText()
                : null;
        }

        private static JsonElement? PropertyIgnoreCase(JsonElement element, string name)
            => element.EnumerateObject().Where(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase) && p.Value.ValueKind != JsonValueKind.Null)
                .Select(p => (JsonElement?) p.Value).FirstOrDefault();
    }
}
