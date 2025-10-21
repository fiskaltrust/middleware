using System;
using fiskaltrust.ifPOS.v1.de;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.DiboldNixdorf;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace fiskaltrust.Middleware.SCU.DE.QueueSimulator
{
    public static class Program
    {
        public static async Task Main()
        {
            try
            {
                var scu = new DiboldNixdorfSCU(new Dictionary<string, object>
            {
                { "comPort", "COM3" }
            });
                var clientId = "POS001";
                await scu.SetTseStateAsync(new TseState
                {
                    CurrentState = TseStates.Initialized
                });

                while (true)
                {
                    try
                    {
                      
                        var startTransactionRequest = new StartTransactionRequest
                        {
                            ClientId = clientId,
                            ProcessType = "KassenBeleg-V1",
                            ProcessDataBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("SuperCoolesZeug"))
                        };
                        var stopWatch = Stopwatch.StartNew();
                        var startTransactionResponse = await scu.StartTransactionExportDataAsync(startTransactionRequest);
                        Console.WriteLine($"Start Transaciton: {startTransactionResponse.TransactionNumber} ({stopWatch.Elapsed})");
                   
                        var finishTransactionRequest = new FinishTransactionRequest
                        {
                            ClientId = clientId,
                            TransactionNumber = startTransactionResponse.TransactionNumber,
                            ProcessType = "KassenBeleg-V1",
                            ProcessDataBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("SuperCoolesZeug"))
                        };
                        stopWatch.Restart();
                        var finishTransactionResponse = await scu.FinishTransactionExportDataAsync(finishTransactionRequest);
                        Console.WriteLine($"Finish Transaciton: {startTransactionResponse.TransactionNumber} ({stopWatch.Elapsed})");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
