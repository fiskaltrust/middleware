namespace fiskaltrust.Middleware.Contracts.Exceptions
{
    public class Errors
    {
        private const string ConnectionError = "[ERR-Connection]";

        public static bool IsConnectionError(string error) => error.StartsWith(ConnectionError);
    }
}
