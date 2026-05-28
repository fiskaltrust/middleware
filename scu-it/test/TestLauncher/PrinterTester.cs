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
            logger.LogInformation("=== Printer test suite against {Url} (auth user/pass={Serial}) ===", deviceUrl, serialForAuth);

            _client = new CustomRTPrinterClient(
                new CustomRTPrinterConfiguration { DeviceUrl = deviceUrl, Username = serialForAuth, Password = serialForAuth, ClientTimeoutMs = 30000 },
                loggerFactory.CreateLogger<CustomRTPrinterClient>());
            var client = _client;

            string serial;
            try
            {
                var info = await client.SendCommand<InfoResp>(new GetInfo());
                serial = string.IsNullOrEmpty(info.SerialNumber) ? serialForAuth : info.SerialNumber;
                Console.WriteLine($"Printer serial: {serial}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FATAL: Could not contact printer at {deviceUrl}: {ex.Message}");
                return;
            }

            Console.WriteLine();
            Console.WriteLine("--- 00. Initial cleanup ---");
            await ResetIfNeeded();

            await Run("01. GetInfo (full)", async () =>
            {
                var info = await client.SendCommand<InfoResp>(new GetInfo());
                return ($"success={info.Success} serial={info.SerialNumber} fiscalized={info.Fiscalized} zSetNumber={info.ZSetNumber}", info.Success);
            });

            await Run("02. QueryPrinterStatus", async () =>
            {
                var st = await client.SendCommand<Response<InfoResp>>(new QueryPrinterStatus());
                return ($"success={st.Success} status={st.Status} printerStatus={st.AddInfo?.PrinterStatus} fpStatus={st.AddInfo?.FpStatus} receiptStep={st.AddInfo?.ReceiptStep}", st.Success);
            });

            string? saleZ = null, saleDoc = null;
            await Run("03. Sale: 1x Caffè @ 1.00 EUR, IVA 22% (idVat=1)", async () =>
            {
                var resp = await client.SendFiscalReceipt<Response<InfoResp>>(new IFiscalRecord[]
                {
                    new PrintRecItem { Description = "Caffe", Quantity = 1000, UnitPrice = 100, IdVat = 1 },
                    new PrintRecTotal { Description = "Contanti", Payment = 100, PaymentType = 1 }
                });
                if (resp.Success) { saleZ = resp.AddInfo?.NClose; saleDoc = resp.AddInfo?.FiscalDoc; }
                return ($"success={resp.Success} status={resp.Status} z={resp.AddInfo?.NClose} doc={resp.AddInfo?.FiscalDoc}", resp.Success);
            });

            await Delay(3000);
            await ResetIfNeeded();

            await Run("04. Sale: 3x Pizza @ 8.00 EUR each (qty>1), IVA 10% (idVat=2)", async () =>
            {
                var resp = await client.SendFiscalReceipt<Response<InfoResp>>(new IFiscalRecord[]
                {
                    new PrintRecItem { Description = "Pizza Margherita", Quantity = 3000, UnitPrice = 800, IdVat = 2 },
                    new PrintRecTotal { Description = "Contanti", Payment = 2400, PaymentType = 1 }
                });
                return ($"success={resp.Success} status={resp.Status} z={resp.AddInfo?.NClose} doc={resp.AddInfo?.FiscalDoc}", resp.Success);
            });

            await Delay(3000);
            await ResetIfNeeded();

            await Run("05. Sale: 7x Bottiglia @ 0.99 EUR (strange total 6.93), IVA 22%", async () =>
            {
                var resp = await client.SendFiscalReceipt<Response<InfoResp>>(new IFiscalRecord[]
                {
                    new PrintRecItem { Description = "Bottiglia 0.5l", Quantity = 7000, UnitPrice = 99, IdVat = 1 },
                    new PrintRecTotal { Description = "Contanti", Payment = 693, PaymentType = 1 }
                });
                return ($"success={resp.Success} status={resp.Status} z={resp.AddInfo?.NClose} doc={resp.AddInfo?.FiscalDoc}", resp.Success);
            });

            await Delay(3000);
            await ResetIfNeeded();

            await Run("06. Sale: mixed VAT (22% + 10% + 4%)", async () =>
            {
                var resp = await client.SendFiscalReceipt<Response<InfoResp>>(new IFiscalRecord[]
                {
                    new PrintRecItem { Description = "Caffe",  Quantity = 1000, UnitPrice = 100, IdVat = 1 }, // 22%
                    new PrintRecItem { Description = "Latte",  Quantity = 1000, UnitPrice = 200, IdVat = 2 }, // 10%
                    new PrintRecItem { Description = "Pane",   Quantity = 1000, UnitPrice =  50, IdVat = 3 }, // 4%
                    new PrintRecTotal { Description = "Contanti", Payment = 350, PaymentType = 1 }
                });
                return ($"success={resp.Success} status={resp.Status} z={resp.AddInfo?.NClose} doc={resp.AddInfo?.FiscalDoc}", resp.Success);
            });

            await Delay(3000);
            await ResetIfNeeded();

            await Run("07. Sale: IVA 5% (idVat=4) 1x @ 1.50 EUR", async () =>
            {
                var resp = await client.SendFiscalReceipt<Response<InfoResp>>(new IFiscalRecord[]
                {
                    new PrintRecItem { Description = "Prodotto IVA5", Quantity = 1000, UnitPrice = 150, IdVat = 4 },
                    new PrintRecTotal { Description = "Contanti", Payment = 150, PaymentType = 1 }
                });
                return ($"success={resp.Success} status={resp.Status} z={resp.AddInfo?.NClose} doc={resp.AddInfo?.FiscalDoc}", resp.Success);
            });

            await Delay(3000);
            await ResetIfNeeded();

            await Run("08. Sale: payment with credit card (paymentType=2)", async () =>
            {
                var resp = await client.SendFiscalReceipt<Response<InfoResp>>(new IFiscalRecord[]
                {
                    new PrintRecItem { Description = "Caffe", Quantity = 2000, UnitPrice = 100, IdVat = 1 },
                    new PrintRecTotal { Description = "Carta", Payment = 200, PaymentType = 2 }
                });
                return ($"success={resp.Success} status={resp.Status} z={resp.AddInfo?.NClose} doc={resp.AddInfo?.FiscalDoc}", resp.Success);
            });

            await Delay(3000);
            await ResetIfNeeded();

            await Run("09. Non-fiscal: print normal + barcode", async () =>
            {
                var resp = await client.SendNonFiscal<Response<InfoResp>>(new INonFiscalRecord[]
                {
                    new PrintNormal { Data = "*** Avviso al cliente ***" },
                    new PrintNormal { Data = "Promozione fine settimana" },
                    new PrintBarCode { Code = "1234567890123", CodeType = 1, HriPosition = 2 }
                });
                return ($"success={resp.Success} status={resp.Status}", resp.Success);
            });

            await Delay(3000);
            await ResetIfNeeded();

            await Run("10. Refund (special, no reference): 1x Caffè @ 1.00 EUR, IVA 22%", async () =>
            {
                var req = new PrinterFiscalReceiptRefundSpecial(
                    new BeginRtDocRefundSpecial { DocDate = DateTime.Now.ToString("ddMMyy") },
                    new IFiscalRecord[] { new PrintRecItem { Description = "Caffe", Quantity = 1000, UnitPrice = 100, IdVat = 1 } });
                var resp = await client.SendAsync<PrinterFiscalReceiptRefundSpecial, Response<InfoResp>>(req);
                return ($"success={resp.Success} status={resp.Status} z={resp.AddInfo?.NClose} doc={resp.AddInfo?.FiscalDoc}", resp.Success);
            });

            await Delay(3000);
            await ResetIfNeeded();

            await Run("11. Void (special, no reference): 1x Caffè @ 1.00 EUR, IVA 22%", async () =>
            {
                var req = new PrinterFiscalReceiptAnnulmentSpecial(
                    new BeginRtDocAnnulmentSpecial { DocDate = DateTime.Now.ToString("ddMMyy") },
                    new IFiscalRecord[] { new PrintRecItem { Description = "Caffe", Quantity = 1000, UnitPrice = 100, IdVat = 1 } });
                var resp = await client.SendAsync<PrinterFiscalReceiptAnnulmentSpecial, Response<InfoResp>>(req);
                return ($"success={resp.Success} status={resp.Status} z={resp.AddInfo?.NClose} doc={resp.AddInfo?.FiscalDoc}", resp.Success);
            });

            await Delay(3000);
            await ResetIfNeeded();

            if (!string.IsNullOrEmpty(saleZ) && !string.IsNullOrEmpty(saleDoc))
            {
                await Run($"12. Refund (with ref Z={saleZ} Doc={saleDoc}): 1x Caffè @ 1.00 EUR", async () =>
                {
                    var req = new PrinterFiscalReceiptRefund(
                        new BeginRtDocRefund
                        {
                            DocRefZ = saleZ!.PadLeft(4, '0'),
                            DocRefNumber = saleDoc!.PadLeft(4, '0'),
                            DocDate = DateTime.Now.ToString("ddMMyy"),
                            FiscalSerial = serial
                        },
                        new IFiscalRecord[] { new PrintRecItem { Description = "Caffe", Quantity = 1000, UnitPrice = 100, IdVat = 1 } });
                    var resp = await client.SendAsync<PrinterFiscalReceiptRefund, Response<InfoResp>>(req);
                    return ($"success={resp.Success} status={resp.Status} z={resp.AddInfo?.NClose} doc={resp.AddInfo?.FiscalDoc}", resp.Success);
                });

                await Delay(3000);
            await ResetIfNeeded();
            }
            else
            {
                _results.Add(("12. Refund (with ref)", false, "skipped — no Z/Doc captured from test 03"));
            }

            await Run("13. X-Report (read-only daily totals)", async () =>
            {
                var resp = await client.SendFiscalReport<Response<InfoResp>>(new PrintXReport());
                return ($"success={resp.Success} status={resp.Status}", resp.Success);
            });

            await Delay(3000);
            await ResetIfNeeded();

            await Run("14. Z-Report (daily closing) — WILL CLOSE THE DAY", async () =>
            {
                var resp = await client.SendFiscalReport<Response<InfoResp>>(new PrintZReport());
                return ($"success={resp.Success} status={resp.Status} z={resp.AddInfo?.NClose}", resp.Success);
            });

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
