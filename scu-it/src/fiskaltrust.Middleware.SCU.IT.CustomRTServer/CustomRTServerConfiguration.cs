using System;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTServer
{
    public class CustomRTServerConfiguration
    {
        public string? ServerUrl { get; set; }
        public string AccountMasterData { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool SendReceiptsSync { get; set; }
        public bool DisabelSSLValidation { get; set; }
    }

    public class AccountMasterData
    {
        public Guid AccountId { get; set; }

        public string? AccountName { get; set; }

        public string? Street { get; set; } 

        public string? Zip { get; set; }

        public string? City { get; set; } 

        public string? Country { get; set; } 

        public string? TaxId { get; set; } 

        public string? VatId { get; set; } 
    }
}
