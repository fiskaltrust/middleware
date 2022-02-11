using System;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Exceptions;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Models;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Models.Commands;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop
{
    public static class CommandRunner
    {
        public static async Task<SeResult> ExecuteAsync(Func<Task<SeResult>> runAsync)
        {
            try
            {
                return await runAsync();
            }
            catch (CryptoVisionProxyException exception)
            {
                return exception.SeResult;
            }
            catch
            {
                throw;
            }
        }

        public static Task<SeResult> ExecuteSimpleCommandAsync(ITseTransportAdapter transportAdapter, TseCommandCodeEnum command)
        {
            return ExecuteAsync(async () =>
            {
                await transportAdapter.ExecuteAsync(new SimpleTseCommand(command));
                return SeResult.ExecutionOk;
            });
        }

        public static async Task<(SeResult, T)> ExecuteAsync<T>(Func<Task<(SeResult, T)>> runAsync)
        {
            try
            {
                return await runAsync();
            }
            catch (CryptoVisionProxyException exception)
            {
                return (exception.SeResult, default(T));
            }
            catch
            {
                throw;
            }
        }

        public static async Task<(SeResult, T1, T2)> ExecuteAsync<T1, T2>(Func<Task<(SeResult, T1, T2)>> runAsync)
        {
            try
            {
                return await runAsync();
            }
            catch (CryptoVisionProxyException exception)
            {
                return (exception.SeResult, default(T1), default(T2));
            }
            catch
            {
                throw;
            }
        }

        public static async Task<(SeResult, T1, T2, T3, T4)> ExecuteAsync<T1, T2, T3, T4>(Func<Task<(SeResult, T1, T2, T3, T4)>> runAsync)
        {
            try
            {
                return await runAsync();
            }
            catch (CryptoVisionProxyException exception)
            {
                return (exception.SeResult, default(T1), default(T2), default(T3), default(T4));
            }
            catch
            {
                throw;
            }
        }
    }
}
