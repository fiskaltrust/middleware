namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models
{
    public class CommandResponse
    {
        public long BufferNo { get; set; }
        public long PacketSeqNo { get; set; }
        public string BufferStatus { get; set; }
        public long BufferDataSize { get; set; }
        public byte[] BufferData { get; set; }
        public string Command { get; set; }
        public TseResult TseResult { get; set; }
    }
}