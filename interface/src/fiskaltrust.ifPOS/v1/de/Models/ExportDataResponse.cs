using System.Collections.Generic;
using System.Runtime.Serialization;

namespace fiskaltrust.ifPOS.v1.de
{
    [DataContract]
    public class ExportDataResponse
    {
        [DataMember(Order = 10)]
        public string TokenId { get; set; }

        /// <summary>
        /// Base64 encoded chunk of data as part of the download.
        /// If MaxChunkSize is 0, no data will be responded. TarFileEndOfFile and TotalTarFileSize are populated anyway.
        /// </summary>
        [DataMember(Order = 20)]
        public string TarFileByteChunkBase64 { get; set; }

        /// <summary>
        /// Signal no more data to download.
        /// </summary>
        [DataMember(Order = 30)]
        public bool TarFileEndOfFile { get; set; }

        /// <summary>
        /// Signal if the server already prepared the all data to download.
        /// If true then the TotalTarFileSize gives size of total download in bytes
        /// </summary>
        [DataMember(Order = 40)]
        public bool TotalTarFileSizeAvailable { get; set; }

        /// <summary>
        /// Total size of TAR-file to be exported in current session.
        /// If the total size is less than 0, the server did not finish to prepare the complete download.
        /// ExportData can be called again to get next chunk.
        /// EndExportSession will throw Exception as long as TotalTarFileSize cannot be served.
        /// </summary>
        [DataMember(Order = 50)]
        public long TotalTarFileSize { get; set; } = -1;

    }
}
