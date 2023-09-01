using System;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTServer
{
    public class CustomRTServerConfiguration
    {
        public string? ServerUrl { get; set; }

        public AccountMasterData? AccountMasterData { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AccountMasterData
    {
        public Guid AccountId { get; set; }

        public string AccountName { get; set; } = string.Empty;

        public string Street { get; set; } = string.Empty;

        public string Zip { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string Country { get; set; } = string.Empty;

        public string TaxId { get; set; } = string.Empty;

        public string VatId { get; set; } = string.Empty;
    }
}