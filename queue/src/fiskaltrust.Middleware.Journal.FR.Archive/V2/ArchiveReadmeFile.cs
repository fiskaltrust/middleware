using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Journal.FR.Archive.Csv;

namespace fiskaltrust.Middleware.Journal.FR.Archive.V2
{
    /// <summary>
    /// <c>README.txt</c> of a v2 sub-archive (archive-v2 §11): explains, in French, how to read this archive and
    /// describes every file and column it holds. It carries no auditable data: the issuer, the period and the counts
    /// are in the manifest and the files, which it points to. Built from the same column definitions as the files,
    /// so it cannot describe a column the archive lacks or miss one it has.
    /// </summary>
    public sealed class ArchiveReadmeFile : ICsvFileDefinition<ArchiveEntry>
    {
        public const string ReadmeFileName = "README.txt";
        private const int Width = 100;

        private readonly IReadOnlyList<IDocumentedFile> _files;

        public ArchiveReadmeFile(IReadOnlyList<IDocumentedFile> files) => _files = files;

        public string FileName => ReadmeFileName;

        public async Task WriteAsync(IReadOnlyList<ArchiveEntry> records, TextWriter writer, CsvOptions options, CancellationToken cancellationToken)
        {
            var text = Build(options.NewLine);
            await writer.WriteAsync(text).ConfigureAwait(false);
        }

        /// <summary>The whole text; lines end with <paramref name="newLine"/>.</summary>
        public string Build(string newLine = "\r\n")
        {
            var lines = new List<string>();
            var sections = new List<(string Number, string Title, Action Body)>();

            void Title(string number, string title) => lines.AddRange(new[] { "", $"== {number} {title} ==", "" });
            void Para(string text) { lines.AddRange(Wrap(text, "")); lines.Add(""); }
            void Item(string text) => lines.AddRange(Wrap(text, "  - ", "    "));
            void Def(string name, string text) => lines.AddRange(Wrap(text, $"  {name} : ", "      "));

            sections.Add(("1.", "Présentation", () =>
            {
                Para("Ce fichier explique comment lire cette archive fiscale. Il ne contient aucune donnée "
                    + "auditable : les données sont dans les fichiers CSV, dans requests.jsonl et dans manifest.json, "
                    + "auquel il renvoie. Il est généré avec l'archive, pour cette archive.");
                Para("L'archive est produite par fiskaltrust.Middleware, module de fiscalisation pour la France "
                    + "(QueueFR, format d'archive version 2). Les clés de manifest.json donnent l'émetteur (siret), "
                    + "la période archivée (periodStart, periodEnd), le premier et le dernier enregistrement "
                    + "(firstRecordMoment, lastRecordMoment), leur nombre (recordCount), le schéma de signature "
                    + "(schema : InfoCert ou LNE) et la date de génération (generatedAt).");
            }));

            sections.Add(("2.", "Lecture des fichiers", () =>
            {
                Item("Format : CSV, encodage UTF-8 sans BOM, séparateur point-virgule (;), champs entre guillemets "
                    + "doubles (\") lorsque nécessaire, première ligne d'en-tête avec le nom des colonnes, fin de "
                    + "ligne CRLF. Exception : requests.jsonl (une requête JSON par ligne, fin de ligne LF).");
                Item("Dates : ISO 8601, en temps universel (UTC), avec les fractions de seconde, par exemple "
                    + "2026-10-12T08:15:42.1230000Z.");
                Item("Nombres : point comme séparateur décimal, sans séparateur de milliers ni symbole monétaire. "
                    + "Montants tels qu'enregistrés, sans arrondi à l'export. Taux en pourcentage (20, 5.5).");
                Item("Booléens : true ou false. Champ vide : la donnée ne s'applique pas à la ligne.");
                Item("Codes : les codes fiskaltrust (ftReceiptCase, ftChargeItemCase, ftPayItemCase, ftState, "
                    + "ftSignatureType) sont écrits en hexadécimal, 0x suivi de 16 chiffres (section 5).");
                Item("Totaux : tous les totaux sont TTC (toutes taxes comprises), sauf les colonnes dont le nom "
                    + "contient HT. Les grands totaux de période et perpétuels sont valorisés TTC.");
                Item("Prix des articles : UnitPrice et Amount de charge-items.csv sont TVA comprise "
                    + "(PricesIncludeVAT vaut true) ; UnitPriceHT, AmountHT et VATAmount donnent le HT et la TVA, "
                    + "et AmountHT + VATAmount = Amount sur chaque ligne.");
                lines.Add("");
            }));

            sections.Add(("3.", "Organisation de l'archive", () =>
            {
                Para("Une archive principale contient une ou plusieurs sous-archives (fichiers "
                    + "subarchive_<numéro de série>_v<version>_<rang>.zip). Chaque sous-archive couvre la part de la "
                    + "période signée par un même certificat de signature, dans une même version de format ; "
                    + "ensemble, elles couvrent toute la période sans chevauchement. Chaque sous-archive contient "
                    + "les fichiers décrits en section 4 et son propre README.txt.");
                Para("Chaque document est signé et chaîné à la signature du document précédent de la même chaîne. "
                    + "Une chaîne par type de document, identifiée par une lettre ; chaque chaîne a son journal :");
                foreach (var journal in ArchiveV2Files.Journals)
                {
                    lines.Add($"  {journal.Letter}  {journal.FrenchName,-42} {journal.FileName}");
                }
                lines.Add("");
                Para("Les fichiers se joignent par ftQueueItemID (identifiant du document), présent dans chaque "
                    + "fichier ; ftReceiptIdentification permet de contrôler la jointure. Une ligne d'article se "
                    + "joint à ses données complémentaires de case-data.csv par ftQueueItemID et Position, avec "
                    + "Owner = ChargeItem.");
            }));

            sections.Add(("4.", "Description des fichiers", () =>
            {
                // The columns every journal file has are described once, so each journal lists only its own.
                var journals = _files.Where(f => f.FileName.StartsWith("journal-", StringComparison.Ordinal)).ToList();
                var common = journals.Count == 0
                    ? new List<(string Name, string Description)>()
                    : journals[0].ColumnDocs.Where(c => journals.All(j => j.ColumnDocs.Any(o => o.Name == c.Name))).ToList();
                var index = 1;
                var commonSection = "";
                if (common.Count > 0)
                {
                    commonSection = $"4.{index}";
                    lines.AddRange(new[] { "", $"== 4.{index++} Colonnes communes à tous les journaux ==", "" });
                    Para("Chaque fichier journal-<lettre>-<nom>.csv commence par ces colonnes, dans cet ordre :");
                    foreach (var (name, description) in common)
                    {
                        Def(name, description);
                    }
                }
                foreach (var file in _files)
                {
                    lines.AddRange(new[] { "", $"== 4.{index++} {file.FileName} ==", "" });
                    Para("Contenu : " + file.Purpose + ".");
                    var own = file.ColumnDocs.ToList();
                    if (journals.Contains(file) && common.Count > 0)
                    {
                        own = own.Where(c => !common.Any(x => x.Name == c.Name)).ToList();
                        Para(own.Count == 0
                            ? $"Colonnes : les colonnes communes (section {commonSection}) uniquement."
                            : $"Colonnes : les colonnes communes (section {commonSection}), puis :");
                    }
                    foreach (var (name, description) in own)
                    {
                        Def(name, description);
                    }
                }
                lines.AddRange(new[] { "", $"== 4.{index++} requests.jsonl ==", "" });
                Para("Contenu : une ligne par document archivé, avec la requête telle que la file l'a enregistrée, "
                    + "octet pour octet. C'est la source à partir de laquelle l'empreinte signée RequestHash se "
                    + "recalcule. Chaque ligne est un objet JSON aux clés suivantes :");
                Def("ftQueueItemID", "Identifiant du document (jointure avec les fichiers CSV).");
                Def("ftQueueRow", "Numéro d'ordre de la requête dans la file.");
                Def("ftReceiptIdentification", "Numéro du document.");
                Def("ftReceiptMoment", "Date et heure du document (UTC).");
                Def("request", "La requête enregistrée, sous forme de chaîne JSON.");
                lines.AddRange(new[] { "", $"== 4.{index} manifest.json, manifest.sig et certificats ==", "" });
                Para("manifest.json décrit l'archive (section 1) et liste chaque fichier avec sa taille, son "
                    + "empreinte SHA-256 et sa signature. manifest.sig est la signature de manifest.json. "
                    + "archive-certificate.cer est le certificat de signature de l'archive ; scu_*.cer est le "
                    + "certificat qui a signé les documents.");
            }));

            sections.Add(("5.", "Listes de valeurs codées", () =>
            {
                lines.AddRange(new[] { "", "== 5.1 Chaînes (colonne Chain) ==", "" });
                foreach (var journal in ArchiveV2Files.Journals)
                {
                    Def(journal.Letter, journal.FrenchName + ".");
                }
                lines.AddRange(new[] { "", "== 5.2 Ventilation des totaux ==", "" });
                Para("Par taux de TVA : CINormal (taux normal, 20 %), CIDiscounted1 (taux réduit 1, 5,5 %), "
                    + "CIDiscounted2 (taux réduit 2, 10 %), CISuperReduced1 (taux super-réduit, 2,1 %), CIZero "
                    + "(taux zéro, exonéré, non imposable), CIUnknown (taux non classé). Par moyen de paiement : "
                    + "PICash (espèces), PINonCash (carte bancaire, chèque, bon d'achat, paiement en ligne et autres "
                    + "moyens non espèces), PIInternal (consommation interne, transfert vers le coffre ou le "
                    + "personnel), PIUnknown (moyen non classé). Les acomptes sont exclus de ces totaux.");
                lines.AddRange(new[] { "", "== 5.3 Codes ftReceiptCase, ftChargeItemCase et ftPayItemCase ==", "" });
                Para("Les 4 premiers chiffres hexadécimaux identifient le pays (4652 = France). Les 4 derniers "
                    + "donnent le cas ; les chiffres intermédiaires portent des options (drapeaux).");
                Para("ftReceiptCase, cas (4 derniers chiffres) :");
                foreach (var (code, label) in ReceiptCases) Def(code, label);
                Para("");
                Para("ftReceiptCase, options (à ajouter au cas) :");
                foreach (var (code, label) in ReceiptFlags) Def(code, label);
                Para("");
                Para("ftChargeItemCase : dernier chiffre = taux de TVA, avant-dernier = type de prestation, "
                    + "deux chiffres précédents = nature de TVA (colonne VATNature).");
                foreach (var (code, label) in VatCases) Def(code, label);
                Para("");
                Para("ftChargeItemCase, options : 0x40000 remise ou supplément, 0x80000 acompte, 0x100000 consigne, "
                    + "0x10000 annulation, 0x20000 remboursement.");
                Para("ftPayItemCase, moyen de paiement (deux derniers chiffres) :");
                foreach (var (code, label) in PayCases) Def(code, label);
                Para("");
                Para("ftPayItemCase, options : 0x10000 annulation, 0x20000 remboursement, 0x80000 acompte, "
                    + "0x100000 devise étrangère, 0x200000 rendu monnaie, 0x400000 pourboire.");
                lines.AddRange(new[] { "", "== 5.4 Natures de TVA (colonne VATNature) ==", "" });
                Para("00 : TVA ordinaire au taux de la ligne. Les autres codes (exonération, autoliquidation, "
                    + "opération hors champ…) suivent la table française de fiskaltrust des natures de TVA.");
                lines.AddRange(new[] { "", "== 5.5 Types d'éléments de signature (colonne ftSignatureType) ==", "" });
                Para("4 derniers chiffres hexadécimaux, notamment :");
                foreach (var (code, label) in SignatureTypes) Def(code, label);
                lines.AddRange(new[] { "", "== 5.6 Codes d'événements JET (colonne EventCode) ==", "" });
                Para("Codes du dictionnaire des événements techniques (référentiel NF525, R19) ; les codes 900 et "
                    + "plus sont propres à l'éditeur.");
                foreach (var (code, label) in JetCodes) Def(code, label);
                lines.Add("");
            }));

            sections.Add(("6.", "Contrôle d'intégrité", () =>
            {
                Item("Archive : vérifier la signature manifest.sig de manifest.json avec la clé publique de "
                    + "archive-certificate.cer, sur l'empreinte SHA-256, selon l'algorithme de la clé "
                    + "signatureAlgorithm de manifest.json (ES256 : ECDSA P-256, signature r||s), puis, pour chaque "
                    + "fichier listé dans manifest.json, sa taille, son empreinte SHA-256 et sa signature.");
                Item("Documents : vérifier la signature de chaque document (colonne Signature) avec le certificat "
                    + "scu_*.cer dont le numéro de série figure dans CertificateSerialNumber.");
                Item("Chaînage : dans chaque journal, PreviousSignature d'un document est égale à la partie "
                    + "signature (après le dernier point) de Signature du document précédent de la même chaîne, "
                    + "et ChainCounter augmente de 1 sans trou. manifest.json (chainAnchors) donne le point de "
                    + "départ de chaque chaîne dans la sous-archive.");
                Item("Requêtes : l'empreinte RequestHash se recalcule à partir de requests.jsonl, selon la forme "
                    + "canonique documentée par fiskaltrust.");
                Item("Outil : l'outil fiskaltrust TaxAuditorTool réalise tous ces contrôles.");
                lines.Add("");
            }));

            var output = new List<string> { "README - Archive fiscale fiskaltrust (France, format version 2)", "", "Sommaire", "" };
            output.AddRange(sections.Select(s => $"  {s.Number} {s.Title}"));
            foreach (var (number, title, body) in sections)
            {
                lines.Clear();
                Title(number, title);
                body();
                output.AddRange(lines);
            }

            // One blank line at most between blocks, and a final new line.
            var compact = new List<string>();
            foreach (var line in output)
            {
                if (line.Length == 0 && compact.Count > 0 && compact[compact.Count - 1].Length == 0)
                {
                    continue;
                }
                compact.Add(line);
            }
            while (compact.Count > 0 && compact[compact.Count - 1].Length == 0)
            {
                compact.RemoveAt(compact.Count - 1);
            }
            return string.Join(newLine, compact) + newLine;
        }

        private static IEnumerable<string> Wrap(string text, string first, string? rest = null)
        {
            rest ??= first;
            var lines = new List<string>();
            var current = new StringBuilder(first);
            var prefixLength = first.Length;
            foreach (var word in text.Split(' ').Where(w => w.Length > 0))
            {
                if (current.Length > prefixLength && current.Length + 1 + word.Length > Width)
                {
                    lines.Add(current.ToString());
                    current = new StringBuilder(rest);
                    prefixLength = rest.Length;
                }
                if (current.Length > prefixLength)
                {
                    current.Append(' ');
                }
                current.Append(word);
            }
            if (current.Length > prefixLength || lines.Count == 0)
            {
                lines.Add(current.ToString().TrimEnd());
            }
            return lines;
        }

        private static readonly (string, string)[] ReceiptCases =
        {
            ("0x0000", "ticket, type non précisé"), ("0x0001", "ticket de caisse"), ("0x0002", "justificatif de paiement"),
            ("0x0003", "ticket sans obligation de délivrance"), ("0x0004", "vente à distance (e-commerce)"), ("0x0005", "bon de livraison"),
            ("0x1000", "facture, type non précisé"), ("0x1001", "facture à un particulier"), ("0x1002", "facture à une entreprise"),
            ("0x1003", "facture à une administration"), ("0x2000", "ticket zéro (sans vente)"), ("0x2001", "ticket de contrôle"),
            ("0x2010", "clôture de service"), ("0x2011", "clôture journalière"), ("0x2012", "clôture mensuelle"),
            ("0x2013", "clôture annuelle"), ("0x3000", "protocole, non précisé"), ("0x3001", "événement technique (JET)"),
            ("0x3002", "événement comptable"), ("0x3003", "consommation interne"), ("0x3004", "commande (note)"),
            ("0x3005", "paiement (justificatif)"), ("0x3010", "duplicata d'un document existant"),
            ("0x4001", "mise en service"), ("0x4002", "mise hors service"),
        };

        private static readonly (string, string)[] ReceiptFlags =
        {
            ("0x10000", "signature différée (document enregistré hors ligne)"), ("0x20000", "mode école (formation)"),
            ("0x40000", "annulation"), ("0x80000", "document manuscrit"), ("0x1000000", "remboursement"),
        };

        private static readonly (string, string)[] VatCases =
        {
            ("0", "taux non précisé"), ("1", "taux réduit 1 (5,5 %)"), ("2", "taux réduit 2 (10 %)"), ("3", "taux normal (20 %)"),
            ("4", "taux super-réduit 1 (2,1 %)"), ("5", "taux super-réduit 2"), ("6", "taux parking"), ("7", "taux zéro"),
            ("8", "non imposable"),
        };

        private static readonly (string, string)[] PayCases =
        {
            ("0x00", "moyen non précisé"), ("0x01", "espèces"), ("0x02", "paiement non espèces"), ("0x03", "chèque"),
            ("0x04", "carte de débit"), ("0x05", "carte de crédit"), ("0x06", "bon d'achat à valeur monétaire"),
            ("0x07", "paiement en ligne"), ("0x08", "carte de fidélité"), ("0x09", "créance client"), ("0x0A", "virement SEPA"),
            ("0x0B", "autre virement"), ("0x0C", "transfert vers le coffre, le propriétaire ou le personnel"),
            ("0x0D", "consommation interne"), ("0x0E", "subvention"), ("0x0F", "titre-restaurant"),
        };

        private static readonly (string, string)[] SignatureTypes =
        {
            ("0x0000", "mention imprimée (information)"), ("0x0001", "QR code de signature du document"),
            ("0x0003", "totaux du jour"), ("0x0004", "totaux du mois"), ("0x0005", "totaux de l'année"),
            ("0x0007", "totaux perpétuels"), ("0x0008", "empreinte de chaînage"), ("0x0009", "numéro SIRET"),
            ("0x000B", "document enregistré non signé"), ("0x0010", "mise en service"), ("0x0011", "mise hors service"),
            ("0x0012", "événement JET à transmettre"),
        };

        private static readonly (string, string)[] JetCodes =
        {
            ("10", "changement de mode de séquence continue"), ("20", "archivage fiscal de période"),
            ("30", "archivage fiscal annuel ou d'exercice"), ("40", "arrêt du terminal, déconnexion"),
            ("50", "clôture de période (jour, mois)"), ("60", "clôture annuelle ou d'exercice"),
            ("70", "début du mode dégradé"), ("80", "démarrage du terminal, connexion"),
            ("90", "défaut d'intégrité des données sécurisées ou de l'archive"), ("100", "début d'une fonction spéciale (mode école)"),
            ("105", "fin d'une fonction spéciale"), ("110", "export de données"), ("120", "fin du mode dégradé"),
            ("123", "renvoi forcé vers le système central"), ("125", "centralisation après le mode dégradé"),
            ("155", "duplicata d'une note"), ("156", "duplicata d'un justificatif de paiement"), ("200", "purge des données"),
            ("205", "purge partielle du JET"), ("250", "installation d'une version majeure"),
            ("270", "modification d'un paramètre de conformité"), ("410", "modification des données d'identification de l'assujetti"),
            ("450", "changement de bi-clé de signature"),
        };
    }
}
