using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Journal.FR.Archive.Csv;

namespace fiskaltrust.Middleware.Journal.FR.Archive.V2
{
    /// <summary>
    /// <c>compliance-parameters.csv</c> (archive-v2 §12, Ref LNE 1.8 Exigence 19): the French compliance parameters
    /// of the middleware, the queue and the SCU in force when the archive is generated. Secrets are never passed in.
    /// </summary>
    internal sealed class ComplianceParametersFile : ICsvFileDefinition<ArchiveEntry>, IDocumentedFile
    {
        private readonly IReadOnlyList<ComplianceParameter> _parameters;

        public ComplianceParametersFile(IReadOnlyList<ComplianceParameter> parameters) => _parameters = parameters;

        public string FileName => ArchiveV2Files.ComplianceParametersFileName;

        public string Purpose => "une ligne par paramètre de conformité française du logiciel, de la file ou du module de signature, avec sa valeur à la génération de l'archive";

        public IReadOnlyList<(string Name, string Description)> ColumnDocs { get; } = new[]
        {
            ("Component", "Composant concerné : Middleware (le logiciel), Queue (la file) ou SCU (le module de signature)."),
            ("Parameter", "Nom du paramètre, tel qu'il est configuré."),
            ("Value", "Valeur du paramètre à la génération de l'archive ; vide s'il n'est pas configuré. Ne donne pas les valeurs antérieures : une modification pendant la période archivée figure dans jet.csv (code 270), qui la date sans garantir l'ancienne valeur."),
        };

        public async Task WriteAsync(IReadOnlyList<ArchiveEntry> records, TextWriter writer, CsvOptions options, CancellationToken cancellationToken)
        {
            var csv = new CsvWriter(writer, options);
            if (options.WriteHeader)
            {
                await csv.WriteRowAsync(ColumnDocs.Select(c => c.Name), cancellationToken).ConfigureAwait(false);
            }
            foreach (var parameter in _parameters)
            {
                await csv.WriteRowAsync(new[] { parameter.Component, parameter.Parameter, parameter.Value ?? string.Empty }, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
