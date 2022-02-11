using System;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.Epson.Exceptions;

namespace fiskaltrust.Middleware.SCU.DE.Epson.Helpers.ExceptionHelper
{
    public static class ExceptionHelper
    {
        private const string ExecutionOk = "EXECUTION_OK";

        public static async Task<T> ThrowIfError<T>(this Task<T> result) where T : EpsonResult
        {
            var epsonResult = (await result.ConfigureAwait(false)).Result;

            if (!epsonResult.Equals(ExecutionOk, StringComparison.InvariantCulture))
            {
                throw new EpsonException(epsonResult);
            }

            return await Task.Run(() => result).ConfigureAwait(false);
        }
    }
}
