namespace fiskaltrust.Middleware.Localization.QueueAT.RequestCommands
{
    public class AddSignatureDecision
    {
        public int Number { get; set; }
        public string Exception { get; set; }
        public bool Signing { get; set; }
        public bool Counting { get; set; }
        public bool ZeroReceipt { get; set; }
    }
}