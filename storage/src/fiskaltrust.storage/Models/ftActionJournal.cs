using System;

namespace fiskaltrust.storage.V0
{
    public class ftActionJournal
    {
        //PK
        //public Guid ftLogId { get; set; }
        public Guid ftActionJournalId { get; set; }

        //FK?
        public Guid ftQueueId { get; set; }

        //FK?
        public Guid ftQueueItemId { get; set; }
        public DateTime Moment { get; set; }

        /// <summary>
        /// <0 .. Meldepflichtig
        /// 0.. fatal error
        /// 0x10 .. error
        /// 0x20 .. warning
        /// 0x30 .. information
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// countrycode-identification-description_replace_space_and_dash_by_underline
        /// </summary>
        public string Type { get; set; }

        public string Message { get; set; }
        public string DataBase64 { get; set; }

        public string DataJson { get; set; }

        public long TimeStamp { get; set; }
    }
}