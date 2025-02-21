namespace fiskaltrust.Middleware.Interface.Client.Http
{
    /// <summary>
    /// Used to pass client options to the underlying HTTP client
    /// </summary>
    public class HttpMESSCDClientOptions : ClientOptions
    {
        public bool? DisableSslValidation { get; set; }
    }
}
