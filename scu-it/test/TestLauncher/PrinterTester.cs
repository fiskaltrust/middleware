using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.IT.CustomRTPrinter;
using fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Clients;
using fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Requests;
using fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Responses;
using Microsoft.Extensions.Logging;

namespace TestLauncher
{
    public static class PrinterTester
    {
        private static readonly List<(string Name, bool Ok, string Detail)> _results = new List<(string, bool, string)>();
        private static CustomRTPrinterClient _client;

        private static async Task ResetIfNeeded()
        {
            try
            {
                var st = await _client.SendCommand<Response<InfoResp>>(new QueryPrinterStatus());
                if (st.AddInfo?.ReceiptStep != "0")
                {
                    Console.WriteLine($"  (resetting: receiptStep={st.AddInfo?.ReceiptStep})");
                    await _client.CancelFiscalReceipt<Response<InfoResp>>();
                    await _client.SendCommand<Response<InfoResp>>(new ResetPrinter());
                    await Task.Delay(1500);
                }
            }
            catch (Exception ex) { Console.WriteLine($"  (reset failed: {ex.Message})"); }
        }

        public static async Task RunAllAsync(string deviceUrl, ILoggerFactory loggerFactory, string serialForAuth)
        {
            var logger = loggerFactory.CreateLogger("PrinterTester");
            logger.LogInformation("=== Subtotal adjustment test suite against {Url} ===", deviceUrl);

            _client = new CustomRTPrinterClient(
                new CustomRTPrinterConfiguration { DeviceUrl = deviceUrl, Username = serialForAuth, Password = serialForAuth, ClientTimeoutMs = 30000 },
                loggerFactory.CreateLogger<CustomRTPrinterClient>());
            var client = _client;

            Console.WriteLine();
            Console.WriteLine("--- Initial cleanup ---");
            await ResetIfNeeded();
            Console.WriteLine("--- Waiting 90s for printer PDF queue to drain... ---");
            await Task.Delay(90000);

            // ---------- Bisect: short vs long description, otherwise identical ----------
            await Run("CF-1ITEM. single item via SCU model with VALID CF", async () =>
            {
                var records = new IFiscalRecord[]
                {
                    new PrintRecItem { Description = "Farmaco", Quantity = 1000, UnitPrice = 500, Department = 1, IdVat = 3 },
                    new FixedLines { Pitch = "B", Description = "SCNDNL89D06E253J" },
                    new PrintRecTotal { Description = "Contanti", Payment = 500, PaymentType = 1 }
                };
                var resp = await client.SendFiscalReceipt<Response<InfoResp>>(records);
                return ($"success={resp.Success} status={resp.Status} z={resp.AddInfo?.NClose} doc={resp.AddInfo?.FiscalDoc}", resp.Success);
            });
            await Delay(15000);
            await ResetIfNeeded();

            await Run("CF-MIRROR. identical to user's XML but via SCU model", async () =>
            {
                var records = new IFiscalRecord[]
                {
                    new PrintRecItem { Description = "Prodotto IVA 22%", Quantity = 1000, UnitPrice = 100, Department = 1, IdVat = 3 },
                    new PrintRecItem { Description = "Prodotto IVA 22%", Quantity = 1000, UnitPrice = 100, Department = 1, IdVat = 3 },
                    new PrintRecItem { Description = "Prodotto IVA 22%", Quantity = 1000, UnitPrice = 100, Department = 1, IdVat = 3 },
                    new FixedLines { Pitch = "B", Description = "SCNDNL89D06E253J" },
                    new PrintRecTotal { Description = "Contanti", Payment = 300, PaymentType = 1 }
                };
                var resp = await client.SendFiscalReceipt<Response<InfoResp>>(records);
                return ($"success={resp.Success} status={resp.Status} z={resp.AddInfo?.NClose} doc={resp.AddInfo?.FiscalDoc}", resp.Success);
            });
            await Delay(15000);
            await ResetIfNeeded();

            // Also verify normal flow (no CF) still works after model change
            await Run("CF-CONTROL. plain receipt with new model (no CF, must still work)", async () =>
            {
                var records = new IFiscalRecord[]
                {
                    new PrintRecItem { Description = "Caffe", Quantity = 1000, UnitPrice = 100, Department = 1, IdVat = 1 },
                    new PrintRecTotal { Description = "Contanti", Payment = 100, PaymentType = 1 }
                };
                var resp = await client.SendFiscalReceipt<Response<InfoResp>>(records);
                return ($"success={resp.Success} status={resp.Status} z={resp.AddInfo?.NClose} doc={resp.AddInfo?.FiscalDoc}", resp.Success);
            });
            await Delay(15000);
            await ResetIfNeeded();

            await Run("CF-RAW. user's exact XML via SendRawAsync", async () =>
            {
                var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<printerFiscalReceipt>
    <dematerializedOn />
    <beginFiscalReceipt></beginFiscalReceipt>
    <printRecItem description=""Prodotto IVA 22%"" unitPrice=""100"" department=""1"" idVat=""3"" quantity=""1000"" />
    <printRecItem description=""Prodotto IVA 22%"" unitPrice=""100"" department=""1"" idVat=""3"" quantity=""1000"" />
    <printRecItem description=""Prodotto IVA 22%"" unitPrice=""100"" department=""1"" idVat=""3"" quantity=""1000"" />
    <fixedLines description=""SCNDNL89D06E253J"" pitch=""B""></fixedLines>
    <printRecTotal description=""Contanti"" payment=""300"" paymentType=""1"" paymentQty=""1"" />
    <endFiscalReceipt></endFiscalReceipt>
    <endFiscalReceiptCut></endFiscalReceiptCut>
</printerFiscalReceipt>";
                var raw = await client.SendRawAsync(xml);
                Console.WriteLine($"  Raw response: {raw}");
                var ok = raw.Contains("success=\"true\"");
                return ($"ok={ok}", ok);
            });
            await Delay(15000);
            await ResetIfNeeded();

            await Run("LOTTERY. Sale 1×Caffè @1€ with setLotteryCode='ABCD1234'", async () =>
            {
                var records = new IFiscalRecord[]
                {
                    new SetLotteryCode { Code = "ABCD1234" },
                    new PrintRecItem { Description = "Caffe", Quantity = 1000, UnitPrice = 100, Department = 1, IdVat = 1 },
                    new PrintRecTotal { Description = "Contanti", Payment = 100, PaymentType = 1 }
                };
                var resp = await client.SendFiscalReceipt<Response<InfoResp>>(records);
                return ($"success={resp.Success} status={resp.Status} z={resp.AddInfo?.NClose} doc={resp.AddInfo?.FiscalDoc}", resp.Success);
            });
            await Delay(15000);
            await ResetIfNeeded();


            PrintSummary();
            return;

            await Run("A. User's working XML (no quantity, no idVat, surcharge type=2)", async () =>
            {
                var records = new IFiscalRecord[]
                {
                    new PrintRecItem { Description = "PANINO", UnitPrice = 350, Department = 1 },
                    new PrintRecItem { Description = "PIZZA",  UnitPrice = 450, Department = 2 },
                    new PrintRecSubtotalAdjustment { AdjustmentType = 2, Description = "maggio subt", Amount = 50 },
                    new PrintRecSubtotal(),
                    new PrintRecTotal { Description = "Payment in Cash", Payment = 600, PaymentType = 1 },
                    new PrintRecTotal { Description = "Payment 4",       Payment = 400, PaymentType = 4 }
                };
                var resp = await client.SendFiscalReceipt<Response<InfoResp>>(records);
                return ($"success={resp.Success} status={resp.Status} z={resp.AddInfo?.NClose} doc={resp.AddInfo?.FiscalDoc}", resp.Success);
            });
            await Delay(4000);
            await ResetIfNeeded();

            // ---------- Test B: SCU-style (with quantity and idVat) + surcharge ----------
            await Run("B. SCU-style: quantity=1000, idVat=1, surcharge type=2", async () =>
            {
                var records = new IFiscalRecord[]
                {
                    new PrintRecItem { Description = "PANINO", Quantity = 1000, UnitPrice = 350, Department = 1, IdVat = 1 },
                    new PrintRecItem { Description = "PIZZA",  Quantity = 1000, UnitPrice = 450, Department = 1, IdVat = 1 },
                    new PrintRecSubtotalAdjustment { AdjustmentType = 2, Description = "Maggiorazione", Amount = 50 },
                    new PrintRecSubtotal(),
                    new PrintRecTotal { Description = "Contanti", Payment = 850, PaymentType = 1 }
                };
                var resp = await client.SendFiscalReceipt<Response<InfoResp>>(records);
                return ($"success={resp.Success} status={resp.Status} z={resp.AddInfo?.NClose} doc={resp.AddInfo?.FiscalDoc}", resp.Success);
            });
            await Delay(4000);
            await ResetIfNeeded();

            // ---------- Test C: SCU-style with discount type=1 ----------
            await Run("C. SCU-style with subtotal discount type=1", async () =>
            {
                var records = new IFiscalRecord[]
                {
                    new PrintRecItem { Description = "PANINO", Quantity = 1000, UnitPrice = 350, Department = 1, IdVat = 1 },
                    new PrintRecItem { Description = "PIZZA",  Quantity = 1000, UnitPrice = 450, Department = 1, IdVat = 1 },
                    new PrintRecSubtotalAdjustment { AdjustmentType = 1, Description = "Sconto", Amount = 50 },
                    new PrintRecSubtotal(),
                    new PrintRecTotal { Description = "Contanti", Payment = 750, PaymentType = 1 }
                };
                var resp = await client.SendFiscalReceipt<Response<InfoResp>>(records);
                return ($"success={resp.Success} status={resp.Status} z={resp.AddInfo?.NClose} doc={resp.AddInfo?.FiscalDoc}", resp.Success);
            });
            await Delay(4000);
            await ResetIfNeeded();

            // ---------- K: bisect description length ----------
            await Run("K. type=4 amount=200, desc='DS Maggiorazione' (16 chars)", async () =>
            {
                var records = new IFiscalRecord[]
                {
                    new PrintRecItem { Description = "Base", Quantity = 1000, UnitPrice = 1000, Department = 1, IdVat = 1 },
                    new PrintRecSubtotalAdjustment { AdjustmentType = 4, Description = "DS Maggiorazione", Amount = 200 },
                    new PrintRecSubtotal(),
                    new PrintRecTotal { Description = "Contanti", Payment = 1200, PaymentType = 1 }
                };
                var resp = await client.SendFiscalReceipt<Response<InfoResp>>(records);
                return ($"success={resp.Success} status={resp.Status}", resp.Success);
            });
            await Delay(4000);
            await ResetIfNeeded();

            await Run("L. type=4 amount=200, desc='DS Maggiorazione Sub' (20 chars)", async () =>
            {
                var records = new IFiscalRecord[]
                {
                    new PrintRecItem { Description = "Base", Quantity = 1000, UnitPrice = 1000, Department = 1, IdVat = 1 },
                    new PrintRecSubtotalAdjustment { AdjustmentType = 4, Description = "DS Maggiorazione Sub", Amount = 200 },
                    new PrintRecSubtotal(),
                    new PrintRecTotal { Description = "Contanti", Payment = 1200, PaymentType = 1 }
                };
                var resp = await client.SendFiscalReceipt<Response<InfoResp>>(records);
                return ($"success={resp.Success} status={resp.Status}", resp.Success);
            });
            await Delay(4000);
            await ResetIfNeeded();

            // ---------- I: final SCU output (type=4 surcharge by amount) for user's failing JSON ----------
            await Run("I. SCU FINAL: 10€ + 2€ surcharge type=4 → 12€", async () =>
            {
                var records = new IFiscalRecord[]
                {
                    new PrintRecItem { Description = "DS Articolo Base", Quantity = 1000, UnitPrice = 1000, Department = 1, IdVat = 1 },
                    new PrintRecSubtotalAdjustment { AdjustmentType = 4, Description = "DS Maggiorazione Subtotale", Amount = 200 },
                    new PrintRecSubtotal(),
                    new PrintRecTotal { Description = "DS Cash", Payment = 1200, PaymentType = 1 }
                };
                var resp = await client.SendFiscalReceipt<Response<InfoResp>>(records);
                return ($"success={resp.Success} status={resp.Status} z={resp.AddInfo?.NClose} doc={resp.AddInfo?.FiscalDoc}", resp.Success);
            });
            await Delay(4000);
            await ResetIfNeeded();

            // ---------- J: final SCU output for subtotal DISCOUNT by amount (type=3) ----------
            await Run("J. SCU FINAL: 10€ + 2€ discount type=3 → 8€", async () =>
            {
                var records = new IFiscalRecord[]
                {
                    new PrintRecItem { Description = "DS Articolo Base", Quantity = 1000, UnitPrice = 1000, Department = 1, IdVat = 1 },
                    new PrintRecSubtotalAdjustment { AdjustmentType = 3, Description = "DS Sconto Subtotale", Amount = 200 },
                    new PrintRecSubtotal(),
                    new PrintRecTotal { Description = "DS Cash", Payment = 800, PaymentType = 1 }
                };
                var resp = await client.SendFiscalReceipt<Response<InfoResp>>(records);
                return ($"success={resp.Success} status={resp.Status} z={resp.AddInfo?.NClose} doc={resp.AddInfo?.FiscalDoc}", resp.Success);
            });
            await Delay(4000);
            await ResetIfNeeded();

            PrintSummary();
        }

        private static async Task Run(string name, Func<Task<(string Detail, bool Ok)>> action)
        {
            Console.WriteLine();
            Console.WriteLine($"--- {name} ---");
            try
            {
                var (detail, ok) = await action();
                _results.Add((name, ok, detail));
                Console.WriteLine($"  → {(ok ? "OK" : "FAIL")} {detail}");
            }
            catch (Exception ex)
            {
                _results.Add((name, false, ex.Message));
                Console.WriteLine($"  → EXCEPTION {ex.Message}");
            }
        }

        private static Task Delay(int ms) => Task.Delay(ms);

        private static void PrintSummary()
        {
            Console.WriteLine();
            Console.WriteLine("================ SUMMARY ================");
            var pass = 0; var fail = 0;
            foreach (var (name, ok, detail) in _results)
            {
                Console.WriteLine($"{(ok ? "[ OK ]" : "[FAIL]")} {name}");
                if (!ok) Console.WriteLine($"        {detail}");
                if (ok) pass++; else fail++;
            }
            Console.WriteLine($"---- Total: {pass} OK, {fail} FAIL ----");
        }
    }
}
