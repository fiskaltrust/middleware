namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Models
{
    public enum SeAuthenticationResult
    {
        authenticationOk = 0,
        authenticationFailed,
        authenticationPinIsBlocked,
        authenticationUnknownUserId,
        authenticationError
    }
}
