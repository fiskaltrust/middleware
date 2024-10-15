namespace fiskaltrust.Middleware.SCU.AT.PrimeSignHSM.Models
{
    public class User
    {
        public string UserId { get; set; }
        public bool Enabled { get; set; }
        public string DefaultKey { get; set; }
    }
}
