using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using CsvHelper;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.IntegrationTest.Helpers.Csv;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.CashpointClosing;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.MasterData;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.SingleRecordings;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.ModuleFormatters;
using fiskaltrust.storage.V0.MasterData;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.IntegrationTest
{
    public class ModuleFormatterTests : IClassFixture<TestDataFixture>
    {
        private readonly TestDataFixture _testDataFixture;

        public ModuleFormatterTests(TestDataFixture testDataFixture)
        {
            _testDataFixture = testDataFixture;
        }

        [Fact]
        public async Task CashpointClosingFormatter_Should_Generate_ValidFiles()
        {
            var targetDirectory = Guid.NewGuid().ToString();
            Directory.CreateDirectory(targetDirectory);
            var zNumber = 3;
            var dailyClosingMoment = new DateTime(2021, 1, 19, 22, 0, 0);
            var testData = _testDataFixture.InfoOrderWithPosReceipt;
            var parameters = GetModuleFormatterParameters();
            parameters.IncludeOrders = false;

            var sut = new CashpointClosingFormatter(Mock.Of<ILogger>(), parameters);
            await sut.ProcessAsync(targetDirectory, testData, new DailyClosingParameters { DailyClosingMoment = dailyClosingMoment, ZNumber = zNumber }, null, null);

            var businessCases = ReadCsvEntries<ZGVTyp>(Path.Combine(targetDirectory, "businesscases.csv"));
            businessCases.Should().HaveCount(2);
            businessCases[0].Z_KASSE_ID.Should().Be("TestCashbox");
            businessCases[0].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            businessCases[0].Z_NR.Should().Be(zNumber);
            businessCases[0].GV_TYP.Should().Be("Umsatz");
            businessCases[0].GV_NAME.Should().Be("");
            businessCases[0].AGENTUR_ID.Should().Be(0);
            businessCases[0].UST_SCHLUESSEL.Should().Be(1);
            businessCases[0].Z_UMS_BRUTTO.Should().Be(3.8M);
            businessCases[0].Z_UMS_NETTO.Should().Be(3.19328M);
            businessCases[0].Z_UST.Should().Be(0.60672M);
            businessCases[1].Z_KASSE_ID.Should().Be("TestCashbox");
            businessCases[1].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            businessCases[1].Z_NR.Should().Be(zNumber);
            businessCases[1].GV_TYP.Should().Be("Umsatz");
            businessCases[1].GV_NAME.Should().Be("");
            businessCases[1].AGENTUR_ID.Should().Be(0);
            businessCases[1].UST_SCHLUESSEL.Should().Be(2);
            businessCases[1].Z_UMS_BRUTTO.Should().Be(9.2M);
            businessCases[1].Z_UMS_NETTO.Should().Be(8.59813M);
            businessCases[1].Z_UST.Should().Be(0.60187M);

            var cashPerCurrencies = ReadCsvEntries<ZWaehrung>(Path.Combine(targetDirectory, "cash_per_currency.csv"));
            cashPerCurrencies.Should().HaveCount(1);
            cashPerCurrencies[0].Z_KASSE_ID.Should().Be("TestCashbox");
            cashPerCurrencies[0].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            cashPerCurrencies[0].Z_NR.Should().Be(zNumber);
            cashPerCurrencies[0].ZAHLART_WAEH.Should().Be("EUR");
            cashPerCurrencies[0].ZAHLART_BETRAG_WAEH.Should().Be(13.00M);

            var paymentTypes = ReadCsvEntries<ZZahlart>(Path.Combine(targetDirectory, "payment.csv"));
            paymentTypes.Should().HaveCount(1);
            paymentTypes[0].Z_KASSE_ID.Should().Be("TestCashbox");
            paymentTypes[0].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            paymentTypes[0].Z_NR.Should().Be(zNumber);
            paymentTypes[0].ZAHLART_TYP.Should().Be("Bar");
            paymentTypes[0].ZAHLART_NAME.Should().Be("");
            paymentTypes[0].Z_ZAHLART_BETRAG.Should().Be(13.00M);

            Directory.Delete(targetDirectory, true);
        }

        [Fact]
        public async Task SingleRecordingFormatter_Should_Generate_ValidFiles()
        {
            var targetDirectory = Guid.NewGuid().ToString();
            Directory.CreateDirectory(targetDirectory);
            var zNumber = 3;
            var dailyClosingMoment = new DateTime(2021, 1, 19, 22, 0, 0, 0);
            var testData = _testDataFixture.InfoOrderWithPosReceipt;
            var parameters = GetModuleFormatterParameters();
            parameters.IncludeOrders = false;

            var sut = new SingleRecordingsFormatter(Mock.Of<ILogger>(), parameters);
            await sut.ProcessAsync(targetDirectory, testData, new DailyClosingParameters { DailyClosingMoment = dailyClosingMoment, ZNumber = zNumber }, new List<Common.Models.ReceiptReferencesGroupedData>(), null);

            var lines = ReadCsvEntries<Bonpos>(Path.Combine(targetDirectory, "lines.csv"));
            lines.Should().HaveCount(2);
            lines[0].Z_KASSE_ID.Should().Be("TestCashbox");
            lines[0].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            lines[0].Z_NR.Should().Be(zNumber);
            lines[0].BON_ID.Should().Be("ftB05#IT3325");
            lines[0].POS_ZEILE.Should().Be("0");
            lines[0].GUTSCHEIN_NR.Should().Be("");
            lines[0].ARTIKELTEXT.Should().Be("Bier 0,5 lt");
            lines[0].POS_TERMINAL_ID.Should().Be("");
            lines[0].GV_TYP.Should().Be("Umsatz");
            lines[0].GV_NAME.Should().Be("");
            lines[0].INHAUS.Should().Be(true);
            lines[0].P_STORNO.Should().Be(false);
            lines[0].AGENTUR_ID.Should().Be(0);
            lines[0].ART_NR.Should().Be("101");
            lines[0].GTIN.Should().Be("");
            lines[0].WARENGR_ID.Should().Be("4057870583");
            lines[0].WARENGR.Should().Be("Bier");
            lines[0].MENGE.Should().Be(1);
            lines[0].FAKTOR.Should().Be(1);
            lines[0].EINHEIT.Should().Be("Liter");
            lines[0].STK_BR.Should().Be(3.8M);
            lines[1].Z_KASSE_ID.Should().Be("TestCashbox");
            lines[1].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            lines[1].Z_NR.Should().Be(zNumber);
            lines[1].BON_ID.Should().Be("ftB05#IT3325");
            lines[1].POS_ZEILE.Should().Be("1");
            lines[1].GUTSCHEIN_NR.Should().Be("");
            lines[1].ARTIKELTEXT.Should().Be("Schnitzel");
            lines[1].POS_TERMINAL_ID.Should().Be("");
            lines[1].GV_TYP.Should().Be("Umsatz");
            lines[1].GV_NAME.Should().Be("");
            lines[1].INHAUS.Should().Be(true);
            lines[1].P_STORNO.Should().Be(false);
            lines[1].AGENTUR_ID.Should().Be(0);
            lines[1].ART_NR.Should().Be("102");
            lines[1].GTIN.Should().Be("");
            lines[1].WARENGR_ID.Should().Be("2008573077");
            lines[1].WARENGR.Should().Be("Speisen");
            lines[1].MENGE.Should().Be(1);
            lines[1].FAKTOR.Should().Be(1);
            lines[1].EINHEIT.Should().Be("Stk");
            lines[1].STK_BR.Should().Be(9.2M);

            var linesVat = ReadCsvEntries<BonposUst>(Path.Combine(targetDirectory, "lines_vat.csv"));
            linesVat.Should().HaveCount(2);
            linesVat[0].Z_KASSE_ID.Should().Be("TestCashbox");
            linesVat[0].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            linesVat[0].Z_NR.Should().Be(zNumber);
            linesVat[0].BON_ID.Should().Be("ftB05#IT3325");
            linesVat[0].POS_ZEILE.Should().Be("0");
            linesVat[0].UST_SCHLUESSEL.Should().Be(1);
            linesVat[0].POS_BRUTTO.Should().Be(3.8M);
            linesVat[0].POS_NETTO.Should().Be(3.19328M);
            linesVat[0].POS_UST.Should().Be(0.60672M);
            linesVat[1].Z_KASSE_ID.Should().Be("TestCashbox");
            linesVat[1].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            linesVat[1].Z_NR.Should().Be(zNumber);
            linesVat[1].BON_ID.Should().Be("ftB05#IT3325");
            linesVat[1].POS_ZEILE.Should().Be("1");
            linesVat[1].UST_SCHLUESSEL.Should().Be(2);
            linesVat[1].POS_BRUTTO.Should().Be(9.2M);
            linesVat[1].POS_NETTO.Should().Be(8.59813M);
            linesVat[1].POS_UST.Should().Be(0.60187M);

            var transactions = ReadCsvEntries<Bonkopf>(Path.Combine(targetDirectory, "transactions.csv"));
            transactions.Should().HaveCount(1);
            transactions[0].Z_KASSE_ID.Should().Be("TestCashbox");
            transactions[0].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            transactions[0].Z_NR.Should().Be(zNumber);
            transactions[0].BON_ID.Should().Be("ftB05#IT3325");
            transactions[0].BON_NR.Should().Be(0xB05);
            transactions[0].BON_TYP.Should().Be("Beleg");
            transactions[0].BON_NAME.Should().Be("");
            transactions[0].TERMINAL_ID.Should().Be("");
            transactions[0].BON_STORNO.Should().Be(false);
            transactions[0].BON_START.ToUniversalTime().Should().Be(new DateTime(2020, 06, 29, 11, 00, 10, DateTimeKind.Utc));
            transactions[0].BON_ENDE.ToUniversalTime().Should().Be(new DateTime(2021, 01, 19, 15, 40, 05, DateTimeKind.Utc));
            transactions[0].BEDIENER_ID.Should().Be("Astrid");
            transactions[0].BEDIENER_NAME.Should().Be("Astrid");
            transactions[0].UMS_BRUTTO.Should().Be(13);
            transactions[0].KUNDE_NAME.Should().Be("");
            transactions[0].KUNDE_ID.Should().Be("");
            transactions[0].KUNDE_TYP.Should().Be("");
            transactions[0].KUNDE_STRASSE.Should().Be("");
            transactions[0].KUNDE_PLZ.Should().Be("");
            transactions[0].KUNDE_ORT.Should().Be("");
            transactions[0].KUNDE_LAND.Should().Be("");
            transactions[0].KUNDE_USTID.Should().Be("");
            transactions[0].BON_NOTIZ.Should().Be("");

            var transactionVats = ReadCsvEntries<BonkopfUst>(Path.Combine(targetDirectory, "transactions_vat.csv"));
            transactionVats.Should().HaveCount(2);
            transactionVats[0].Z_KASSE_ID.Should().Be("TestCashbox");
            transactionVats[0].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            transactionVats[0].Z_NR.Should().Be(zNumber);
            transactionVats[0].BON_ID.Should().Be("ftB05#IT3325");
            transactionVats[0].UST_SCHLUESSEL.Should().Be(1);
            transactionVats[0].BON_BRUTTO.Should().Be(3.8M);
            transactionVats[0].BON_NETTO.Should().Be(Math.Round(3.19328M, 2));
            transactionVats[0].BON_UST.Should().Be(Math.Round(0.60672M, 2));
            transactionVats[1].Z_KASSE_ID.Should().Be("TestCashbox");
            transactionVats[1].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            transactionVats[1].Z_NR.Should().Be(zNumber);
            transactionVats[1].BON_ID.Should().Be("ftB05#IT3325");
            transactionVats[1].UST_SCHLUESSEL.Should().Be(2);
            transactionVats[1].BON_BRUTTO.Should().Be(9.2M);
            transactionVats[1].BON_NETTO.Should().Be(Math.Round(8.59813M, 2));
            ;
            transactionVats[1].BON_UST.Should().Be(Math.Round(0.60187M, 2));

            var dataPayments = ReadCsvEntries<BonkopfZahlart>(Path.Combine(targetDirectory, "datapayment.csv"));
            dataPayments.Should().HaveCount(1);
            dataPayments[0].Z_KASSE_ID.Should().Be("TestCashbox");
            dataPayments[0].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            dataPayments[0].Z_NR.Should().Be(zNumber);
            dataPayments[0].BON_ID.Should().Be("ftB05#IT3325");
            dataPayments[0].ZAHLART_TYP.Should().Be("Bar");
            dataPayments[0].ZAHLART_NAME.Should().Be("Bar");
            dataPayments[0].ZAHLWAEH_CODE.Should().Be("");
            dataPayments[0].ZAHLWAEH_BETRAG.Should().Be(null);
            dataPayments[0].BASISWAEH_BETRAG.Should().Be(13.00M);

            var references = ReadCsvEntries<BonReferenz>(Path.Combine(targetDirectory, "references.csv"));
            references.Should().HaveCount(0);

            var tseTransactions = ReadCsvEntries<TseTransaktion>(Path.Combine(targetDirectory, "transactions_tse.csv"));
            tseTransactions.Should().HaveCount(2);
            tseTransactions[0].Z_KASSE_ID.Should().Be("TestCashbox");
            tseTransactions[0].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            tseTransactions[0].Z_NR.Should().Be(zNumber);
            tseTransactions[0].BON_ID.Should().Be("ftB04#IT3324");
            tseTransactions[0].TSE_ID.Should().Be(1);
            tseTransactions[0].TSE_TANR.Should().Be(3324);
            tseTransactions[0].TSE_TA_START.Should().Be(new DateTime(2021, 01, 19, 15, 21, 46));
            tseTransactions[0].TSE_TA_ENDE.Should().Be(new DateTime(2021, 01, 19, 15, 21, 46));
            tseTransactions[0].TSE_TA_VORGANGSART.Should().Be("Bestellung-V1");
            tseTransactions[0].TSE_TA_SIGZ.Should().Be(6626);
            tseTransactions[0].TSE_TA_SIG.Should().Be("u/Zm8xdnFD4AtEhbJLYUWrIdA057b4ccmIf0NS8v6+o8ZEchARzeFroZMjrk46efEBrYz4Saw9ARSUq71M8oDw==");
            tseTransactions[0].TSE_TA_FEHLER.Should().Be("");
            tseTransactions[0].TSE_VORGANGSDATEN.Should().Be("1;\"Bier 0,5 lt\";3.80\n1;\"Schnitzel\";9.20\n");
            tseTransactions[1].Z_KASSE_ID.Should().Be("TestCashbox");
            tseTransactions[1].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            tseTransactions[1].Z_NR.Should().Be(zNumber);
            tseTransactions[1].BON_ID.Should().Be("ftB05#IT3325");
            tseTransactions[1].TSE_ID.Should().Be(1);
            tseTransactions[1].TSE_TANR.Should().Be(3325);
            tseTransactions[1].TSE_TA_START.Should().Be(new DateTime(2021, 01, 19, 15, 40, 05));
            tseTransactions[1].TSE_TA_ENDE.Should().Be(new DateTime(2021, 01, 19, 15, 40, 05));
            tseTransactions[1].TSE_TA_VORGANGSART.Should().Be("Kassenbeleg-V1");
            tseTransactions[1].TSE_TA_SIGZ.Should().Be(6628);
            tseTransactions[1].TSE_TA_SIG.Should().Be("YZ5ey0a2ir3X8iTcA2i8lCipbKw/keQxWGttKUzWcAZQ5HvzUNU8Py0xvHwuPylDzv7PqLw9t5EJTZJjADNhrw==");
            tseTransactions[1].TSE_TA_FEHLER.Should().Be("");
            tseTransactions[1].TSE_VORGANGSDATEN.Should().Be("Beleg^3.80_9.20_0.00_0.00_0.00^13.00:Bar");

            var allocationGroups = ReadCsvEntries<BonkopfAbrKreis>(Path.Combine(targetDirectory, "allocation_groups.csv"));
            allocationGroups.Should().HaveCount(2);
            allocationGroups[0].Z_KASSE_ID.Should().Be("TestCashbox");
            allocationGroups[0].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            allocationGroups[0].Z_NR.Should().Be(zNumber);
            allocationGroups[0].BON_ID.Should().Be("ftB04#IT3324");
            allocationGroups[0].ABRECHNUNGSKREIS.Should().Be("R1");
            allocationGroups[1].Z_KASSE_ID.Should().Be("TestCashbox");
            allocationGroups[1].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            allocationGroups[1].Z_NR.Should().Be(zNumber);
            allocationGroups[1].BON_ID.Should().Be("ftB05#IT3325");
            allocationGroups[1].ABRECHNUNGSKREIS.Should().Be("Tisch 1");

            Directory.Delete(targetDirectory, true);
        }

        [Fact]
        public async Task MasterDataFormatter_Should_Generate_ValidFiles()
        {
            var targetDirectory = Guid.NewGuid().ToString();
            Directory.CreateDirectory(targetDirectory);
            var zNumber = 3;
            var dailyClosingMoment = new DateTime(2021, 1, 19, 22, 0, 0);
            var testData = _testDataFixture.InfoOrderWithPosReceipt.Concat(_testDataFixture.DailyClosingReceipt).ToList();
            var parameters = GetModuleFormatterParameters();
            parameters.IncludeOrders = false;
            var sut = new MasterDataModuleFormatter(Mock.Of<ILogger>(), parameters, GetMasterDataConfiguration());
            await sut.ProcessAsync(targetDirectory, testData, new DailyClosingParameters { DailyClosingMoment = dailyClosingMoment, ZNumber = zNumber });

            var cashpointClosings = ReadCsvEntries<StammAbschluss>(Path.Combine(targetDirectory, "cashpointclosing.csv"));
            cashpointClosings.Should().HaveCount(1);
            cashpointClosings[0].Z_KASSE_ID.Should().Be("TestCashbox");
            cashpointClosings[0].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            cashpointClosings[0].Z_NR.Should().Be(zNumber);
            cashpointClosings[0].Z_BUCHUNGSTAG.Should().Be("");
            cashpointClosings[0].TAXONOMIE_VERSION.Should().Be("2.3");
            cashpointClosings[0].Z_START_ID.Should().Be("ftB04#IT3324");
            cashpointClosings[0].Z_ENDE_ID.Should().Be("ftB06#IT3326");
            cashpointClosings[0].NAME.Should().Be("_demo_posdealer_company 12345");
            cashpointClosings[0].STRASSE.Should().Be("Alpenstraße 99/2");
            cashpointClosings[0].PLZ.Should().Be("5020");
            cashpointClosings[0].ORT.Should().Be("Salzburg");
            cashpointClosings[0].LAND.Should().Be("AUT");
            cashpointClosings[0].STNR.Should().Be("demo-TAXposdealer");
            cashpointClosings[0].USTID.Should().Be("demo-ATUposdeal");
            cashpointClosings[0].Z_SE_ZAHLUNGEN.Should().Be(13.00M);
            cashpointClosings[0].Z_SE_BARZAHLUNGEN.Should().Be(13.00M);

            var locations = ReadCsvEntries<StammOrt>(Path.Combine(targetDirectory, "location.csv"));
            locations.Should().HaveCount(1);
            locations[0].Z_KASSE_ID.Should().Be("TestCashbox");
            locations[0].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            locations[0].Z_NR.Should().Be(zNumber);
            locations[0].LOC_NAME.Should().Be("My First and most important outlet");
            locations[0].LOC_STRASSE.Should().Be("Alpenstraße 99/2");
            locations[0].LOC_PLZ.Should().Be("5020");
            locations[0].LOC_ORT.Should().Be("Salzburg");
            locations[0].LOC_LAND.Should().Be("AUT");
            locations[0].LOC_USTID.Should().Be("demo-ATUposdeal");

            var cashregisters = ReadCsvEntries<StammKasse>(Path.Combine(targetDirectory, "cashregister.csv"));
            cashregisters.Should().HaveCount(1);
            cashregisters[0].Z_KASSE_ID.Should().Be("TestCashbox");
            cashregisters[0].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            cashregisters[0].Z_NR.Should().Be(zNumber);
            cashregisters[0].KASSE_BRAND.Should().Be("pos system 1 brand");
            cashregisters[0].KASSE_MODELL.Should().Be("pos system 1 type");
            cashregisters[0].KASSE_SERIENNR.Should().Be("TestCashbox");
            cashregisters[0].KASSE_SW_BRAND.Should().Be("fiskaltrust.Middleware");
            cashregisters[0].KASSE_SW_VERSION.Should().Be("1.3");
            cashregisters[0].KASSE_BASISWAEH_CODE.Should().Be("EUR");
            cashregisters[0].KEINE_UST_ZUORDNUNG.Should().Be("0");

            var terminals = ReadCsvEntries<StammTerminal>(Path.Combine(targetDirectory, "slaves.csv"));
            terminals.Should().HaveCount(0);

            var agencies = ReadCsvEntries<StammAgentur>(Path.Combine(targetDirectory, "pa.csv"));
            agencies.Should().HaveCount(11);
            agencies[0].Z_KASSE_ID.Should().Be("TestCashbox");
            agencies[0].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            agencies[0].Z_NR.Should().Be(zNumber);
            agencies[0].AGENTUR_ID.Should().Be(1);
            agencies[0].AGENTUR_NAME.Should().Be("Autobahnen- und Schnellstraßen- Finanzierungs-Aktiengesellsc");
            agencies[0].AGENTUR_STRASSE.Should().Be("Rotenturmstraße 5-9, Postfach 983");
            agencies[0].AGENTUR_PLZ.Should().Be("1011");
            agencies[0].AGENTUR_ORT.Should().Be("Wien");
            agencies[0].AGENTUR_LAND.Should().Be("AUT");
            agencies[0].AGENTUR_STNR.Should().Be("0");
            agencies[0].AGENTUR_USTID.Should().Be("ATU43143200");

            var vats = ReadCsvEntries<StammUst>(Path.Combine(targetDirectory, "vat.csv"));
            vats.Should().HaveCount(2);
            vats[0].Z_KASSE_ID.Should().Be("TestCashbox");
            vats[0].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            vats[0].Z_NR.Should().Be(zNumber);
            vats[0].UST_SCHLUESSEL.Should().Be(1);
            vats[0].UST_SATZ.Should().Be(19.00M);
            vats[0].UST_BESCHR.Should().Be("19,00 % Allgemeiner Steuersatz");
            vats[1].Z_KASSE_ID.Should().Be("TestCashbox");
            vats[1].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            vats[1].Z_NR.Should().Be(zNumber);
            vats[1].UST_SCHLUESSEL.Should().Be(2);
            vats[1].UST_SATZ.Should().Be(7.00M);
            vats[1].UST_BESCHR.Should().Be("7,00 % Ermäßigter Steuersatz");

            var tses = ReadCsvEntries<StammTSE>(Path.Combine(targetDirectory, "tse.csv"));
            tses.Should().HaveCount(1);
            tses[0].Z_KASSE_ID.Should().Be("TestCashbox");
            tses[0].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            tses[0].Z_NR.Should().Be(zNumber);

            tses[0].TSE_ID.Should().Be(1);
            tses[0].TSE_SERIAL.Should().Be("fc976f6020e0758056b2ac494bcc005bd0ccbb8bf34970ce8bd39ff2bd6c961a");
            tses[0].TSE_SIG_ALGO.Should().Be("ecdsa-plain-SHA256");
            tses[0].TSE_ZEITFORMAT.Should().Be("utcTime");
            tses[0].TSE_PD_ENCODING.Should().Be("UTF-8");
            tses[0].TSE_PUBLIC_KEY.Should().Be("MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE+rAm8qU9TzDGoJHW7jP0ZnlKiOrSpe4dr1tUHBInYVrySwjgtEM4gNC29OSjxYyJgEKhuxK/UN4L2bWvQzeD5A==");
            tses[0].TSE_ZERTIFIKAT_I.Should().Be("MIICHTCCAcKgAwIBAgIhAPyXb2Ag4HWAVrKsSUvMAFvQzLuL80lwzovTn/K9bJYaMAoGCCqGSM49BAMCMF0xCzAJBgNVBAYTAkFUMQ8wDQYDVQQHDAZWaWVubmExFTATBgNVBAoMDGZpc2thbHkgR21iSDEmMCQGA1UEAwwdZmlza2FseSBzaWduIFRlc3QgQ2VydGlmaWNhdGUwHhcNMjAwODAzMTQyNzEzWhcNMzAwODAxMTQyNzEzWjBdMQswCQYDVQQGEwJBVDEPMA0GA1UEBwwGVmllbm5hMRUwEwYDVQQKDAxmaXNrYWx5IEdtYkgxJjAkBgNVBAMMHWZpc2thbHkgc2lnbiBUZXN0IENlcnRpZmljYXRlMFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE+rAm8qU9TzDGoJHW7jP0ZnlKiOrSpe4dr1tUHBInYVrySwjgtEM4gNC29OSjxYyJgEKhuxK/UN4L2bWvQzeD5KNTMFEwHQYDVR0OBBYEFO3mYd7qEtFMeatup/tqPBP/yKmzMB8GA1UdIwQYMBaAFO3mYd7qEtFMeatup/tqPBP/yKmzMA8GA1UdEwEB/wQFMAMBAf8wCgYIKoZIzj0EAwIDSQAwRgIhAMt1Dhgdec0WFFm3Z63D8YOEu50984LnlWyqYReEJvidAiEA1NJib9VaVX1PhxxcPMrMTJidgugfuKsBezxLZta+PyE=");
            tses[0].TSE_ZERTIFIKAT_II.Should().Be("");
            tses[0].TSE_ZERTIFIKAT_III.Should().Be("");
            tses[0].TSE_ZERTIFIKAT_IV.Should().Be("");
            tses[0].TSE_ZERTIFIKAT_V.Should().Be("");

            Directory.Delete(targetDirectory, true);
        }

        [Fact]
        public async Task SingleRecordingFormatter_WithOrders_ValidFiles()
        {
            var targetDirectory = Guid.NewGuid().ToString();
            Directory.CreateDirectory(targetDirectory);
            var zNumber = 3;
            var dailyClosingMoment = new DateTime(2021, 1, 19, 22, 0, 0, 0);
            var testData = _testDataFixture.InfoOrderWithPosReceipt;
            var parameters = GetModuleFormatterParameters();
            parameters.IncludeOrders = true;

            var sut = new SingleRecordingsFormatter(Mock.Of<ILogger>(), parameters);
            await sut.ProcessAsync(targetDirectory, testData, new DailyClosingParameters { DailyClosingMoment = dailyClosingMoment, ZNumber = zNumber }, new List<Common.Models.ReceiptReferencesGroupedData>(), null);

            var lines = ReadCsvEntries<Bonpos>(Path.Combine(targetDirectory, "lines.csv"));
            lines.Should().HaveCount(4);

            lines[0].Z_KASSE_ID.Should().Be("TestCashbox");
            lines[0].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            lines[0].Z_NR.Should().Be(zNumber);
            lines[0].BON_ID.Should().Be("ftB04#IT3324");
            lines[0].POS_ZEILE.Should().Be("0");
            lines[0].GUTSCHEIN_NR.Should().Be("");
            lines[0].ARTIKELTEXT.Should().Be("Bier 0,5 lt");
            lines[0].POS_TERMINAL_ID.Should().Be("");
            lines[0].GV_TYP.Should().Be("Umsatz");
            lines[0].GV_NAME.Should().Be("");
            lines[0].INHAUS.Should().Be(true);
            lines[0].P_STORNO.Should().Be(false);
            lines[0].AGENTUR_ID.Should().Be(0);
            lines[0].ART_NR.Should().Be("101");
            lines[0].GTIN.Should().Be("");
            lines[0].WARENGR_ID.Should().Be("4057870583");
            lines[0].WARENGR.Should().Be("Bier");
            lines[0].MENGE.Should().Be(1);
            lines[0].FAKTOR.Should().Be(1);
            lines[0].EINHEIT.Should().Be("Liter");
            lines[0].STK_BR.Should().Be(3.8M);
            lines[1].Z_KASSE_ID.Should().Be("TestCashbox");
            lines[1].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            lines[1].Z_NR.Should().Be(zNumber);
            lines[1].BON_ID.Should().Be("ftB04#IT3324");
            lines[1].POS_ZEILE.Should().Be("1");
            lines[1].GUTSCHEIN_NR.Should().Be("");
            lines[1].ARTIKELTEXT.Should().Be("Schnitzel");
            lines[1].POS_TERMINAL_ID.Should().Be("");
            lines[1].GV_TYP.Should().Be("Umsatz");
            lines[1].GV_NAME.Should().Be("");
            lines[1].INHAUS.Should().Be(true);
            lines[1].P_STORNO.Should().Be(false);
            lines[1].AGENTUR_ID.Should().Be(0);
            lines[1].ART_NR.Should().Be("102");
            lines[1].GTIN.Should().Be("");
            lines[1].WARENGR_ID.Should().Be("2008573077");
            lines[1].WARENGR.Should().Be("Speisen");
            lines[1].MENGE.Should().Be(1);
            lines[1].FAKTOR.Should().Be(1);
            lines[1].EINHEIT.Should().Be("Stk");
            lines[1].STK_BR.Should().Be(9.2M);


            lines[2].Z_KASSE_ID.Should().Be("TestCashbox");
            lines[2].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            lines[2].Z_NR.Should().Be(zNumber);
            lines[2].BON_ID.Should().Be("ftB05#IT3325");
            lines[2].POS_ZEILE.Should().Be("0");
            lines[2].GUTSCHEIN_NR.Should().Be("");
            lines[2].ARTIKELTEXT.Should().Be("Bier 0,5 lt");
            lines[2].POS_TERMINAL_ID.Should().Be("");
            lines[2].GV_TYP.Should().Be("Umsatz");
            lines[2].GV_NAME.Should().Be("");
            lines[2].INHAUS.Should().Be(true);
            lines[2].P_STORNO.Should().Be(false);
            lines[2].AGENTUR_ID.Should().Be(0);
            lines[2].ART_NR.Should().Be("101");
            lines[2].GTIN.Should().Be("");
            lines[2].WARENGR_ID.Should().Be("4057870583");
            lines[2].WARENGR.Should().Be("Bier");
            lines[2].MENGE.Should().Be(1);
            lines[2].FAKTOR.Should().Be(1);
            lines[2].EINHEIT.Should().Be("Liter");
            lines[2].STK_BR.Should().Be(3.8M);
            lines[3].Z_KASSE_ID.Should().Be("TestCashbox");
            lines[3].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            lines[3].Z_NR.Should().Be(zNumber);
            lines[3].BON_ID.Should().Be("ftB05#IT3325");
            lines[3].POS_ZEILE.Should().Be("1");
            lines[3].GUTSCHEIN_NR.Should().Be("");
            lines[3].ARTIKELTEXT.Should().Be("Schnitzel");
            lines[3].POS_TERMINAL_ID.Should().Be("");
            lines[3].GV_TYP.Should().Be("Umsatz");
            lines[3].GV_NAME.Should().Be("");
            lines[3].INHAUS.Should().Be(true);
            lines[3].P_STORNO.Should().Be(false);
            lines[3].AGENTUR_ID.Should().Be(0);
            lines[3].ART_NR.Should().Be("102");
            lines[3].GTIN.Should().Be("");
            lines[3].WARENGR_ID.Should().Be("2008573077");
            lines[3].WARENGR.Should().Be("Speisen");
            lines[3].MENGE.Should().Be(1);
            lines[3].FAKTOR.Should().Be(1);
            lines[3].EINHEIT.Should().Be("Stk");
            lines[3].STK_BR.Should().Be(9.2M);

            var linesVat = ReadCsvEntries<BonposUst>(Path.Combine(targetDirectory, "lines_vat.csv"));
            linesVat.Should().HaveCount(4);
            linesVat[0].Z_KASSE_ID.Should().Be("TestCashbox");
            linesVat[0].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            linesVat[0].Z_NR.Should().Be(zNumber);
            linesVat[0].BON_ID.Should().Be("ftB04#IT3324");
            linesVat[0].POS_ZEILE.Should().Be("0");
            linesVat[0].UST_SCHLUESSEL.Should().Be(1);
            linesVat[0].POS_BRUTTO.Should().Be(3.8M);
            linesVat[0].POS_NETTO.Should().Be(3.19328M);
            linesVat[0].POS_UST.Should().Be(0.60672M);
            linesVat[1].Z_KASSE_ID.Should().Be("TestCashbox");
            linesVat[1].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            linesVat[1].Z_NR.Should().Be(zNumber);
            linesVat[1].BON_ID.Should().Be("ftB04#IT3324");
            linesVat[1].POS_ZEILE.Should().Be("1");
            linesVat[1].UST_SCHLUESSEL.Should().Be(2);
            linesVat[1].POS_BRUTTO.Should().Be(9.2M);
            linesVat[1].POS_NETTO.Should().Be(8.59813M);
            linesVat[1].POS_UST.Should().Be(0.60187M);
            linesVat[2].Z_KASSE_ID.Should().Be("TestCashbox");
            linesVat[2].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            linesVat[2].Z_NR.Should().Be(zNumber);
            linesVat[2].BON_ID.Should().Be("ftB05#IT3325");
            linesVat[2].POS_ZEILE.Should().Be("0");
            linesVat[2].UST_SCHLUESSEL.Should().Be(1);
            linesVat[2].POS_BRUTTO.Should().Be(3.8M);
            linesVat[2].POS_NETTO.Should().Be(3.19328M);
            linesVat[2].POS_UST.Should().Be(0.60672M);
            linesVat[3].Z_KASSE_ID.Should().Be("TestCashbox");
            linesVat[3].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            linesVat[3].Z_NR.Should().Be(zNumber);
            linesVat[3].BON_ID.Should().Be("ftB05#IT3325");
            linesVat[3].POS_ZEILE.Should().Be("1");
            linesVat[3].UST_SCHLUESSEL.Should().Be(2);
            linesVat[3].POS_BRUTTO.Should().Be(9.2M);
            linesVat[3].POS_NETTO.Should().Be(8.59813M);
            linesVat[3].POS_UST.Should().Be(0.60187M);

            var transactions = ReadCsvEntries<Bonkopf>(Path.Combine(targetDirectory, "transactions.csv"));
            transactions.Should().HaveCount(2);
            transactions[0].BON_ID.Should().Be("ftB04#IT3324");
            transactions[0].UMS_BRUTTO.Should().Be(13);
            transactions[1].Z_KASSE_ID.Should().Be("TestCashbox");
            transactions[1].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            transactions[1].Z_NR.Should().Be(zNumber);
            transactions[1].BON_ID.Should().Be("ftB05#IT3325");
            transactions[1].BON_NR.Should().Be(0xB05);
            transactions[1].BON_TYP.Should().Be("Beleg");
            transactions[1].BON_NAME.Should().Be("");
            transactions[1].TERMINAL_ID.Should().Be("");
            transactions[1].BON_STORNO.Should().Be(false);
            transactions[1].BON_START.ToUniversalTime().Should().Be(new DateTime(2020, 06, 29, 11, 00, 10, DateTimeKind.Utc));
            transactions[1].BON_ENDE.ToUniversalTime().Should().Be(new DateTime(2021, 01, 19, 15, 40, 05, DateTimeKind.Utc));
            transactions[1].BEDIENER_ID.Should().Be("Astrid");
            transactions[1].BEDIENER_NAME.Should().Be("Astrid");
            transactions[1].UMS_BRUTTO.Should().Be(13);
            transactions[1].KUNDE_NAME.Should().Be("");
            transactions[1].KUNDE_ID.Should().Be("");
            transactions[1].KUNDE_TYP.Should().Be("");
            transactions[1].KUNDE_STRASSE.Should().Be("");
            transactions[1].KUNDE_PLZ.Should().Be("");
            transactions[1].KUNDE_ORT.Should().Be("");
            transactions[1].KUNDE_LAND.Should().Be("");
            transactions[1].KUNDE_USTID.Should().Be("");
            transactions[1].BON_NOTIZ.Should().Be("");

            var transactionVats = ReadCsvEntries<BonkopfUst>(Path.Combine(targetDirectory, "transactions_vat.csv"));
            // The order (Bestellung-V1) holds the cumulated values of its positions, so Bonkopf_USt and Bonpos_USt reconcile (DSFinV-K 2.4 Tz. 3.1.2).
            transactionVats.Should().HaveCount(4);
            transactionVats[0].BON_ID.Should().Be("ftB04#IT3324");
            transactionVats[0].UST_SCHLUESSEL.Should().Be(1);
            transactionVats[0].BON_BRUTTO.Should().Be(3.8M);
            transactionVats[0].BON_NETTO.Should().Be(Math.Round(3.19328M, 2));
            transactionVats[0].BON_UST.Should().Be(Math.Round(0.60672M, 2));
            transactionVats[1].BON_ID.Should().Be("ftB04#IT3324");
            transactionVats[1].UST_SCHLUESSEL.Should().Be(2);
            transactionVats[1].BON_BRUTTO.Should().Be(9.2M);
            transactionVats[1].BON_NETTO.Should().Be(Math.Round(8.59813M, 2));
            transactionVats[1].BON_UST.Should().Be(Math.Round(0.60187M, 2));
            transactionVats[2].Z_KASSE_ID.Should().Be("TestCashbox");
            transactionVats[2].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            transactionVats[2].Z_NR.Should().Be(zNumber);
            transactionVats[2].BON_ID.Should().Be("ftB05#IT3325");
            transactionVats[2].UST_SCHLUESSEL.Should().Be(1);
            transactionVats[2].BON_BRUTTO.Should().Be(3.8M);
            transactionVats[2].BON_NETTO.Should().Be(Math.Round(3.19328M, 2));
            transactionVats[2].BON_UST.Should().Be(Math.Round(0.60672M, 2));
            transactionVats[3].Z_KASSE_ID.Should().Be("TestCashbox");
            transactionVats[3].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            transactionVats[3].Z_NR.Should().Be(zNumber);
            transactionVats[3].BON_ID.Should().Be("ftB05#IT3325");
            transactionVats[3].UST_SCHLUESSEL.Should().Be(2);
            transactionVats[3].BON_BRUTTO.Should().Be(9.2M);
            transactionVats[3].BON_NETTO.Should().Be(Math.Round(8.59813M, 2));
            transactionVats[3].BON_UST.Should().Be(Math.Round(0.60187M, 2));

            var dataPayments = ReadCsvEntries<BonkopfZahlart>(Path.Combine(targetDirectory, "datapayment.csv"));
            dataPayments.Should().HaveCount(2);
            dataPayments[1].Z_KASSE_ID.Should().Be("TestCashbox");
            dataPayments[1].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            dataPayments[1].Z_NR.Should().Be(zNumber);
            dataPayments[1].BON_ID.Should().Be("ftB05#IT3325");
            dataPayments[1].ZAHLART_TYP.Should().Be("Bar");
            dataPayments[1].ZAHLART_NAME.Should().Be("Bar");
            dataPayments[1].ZAHLWAEH_CODE.Should().Be("");
            dataPayments[1].ZAHLWAEH_BETRAG.Should().Be(null);
            dataPayments[1].BASISWAEH_BETRAG.Should().Be(13.00M);

            var references = ReadCsvEntries<BonReferenz>(Path.Combine(targetDirectory, "references.csv"));
            references.Should().HaveCount(0);

            var tseTransactions = ReadCsvEntries<TseTransaktion>(Path.Combine(targetDirectory, "transactions_tse.csv"));
            tseTransactions.Should().HaveCount(2);
            tseTransactions[0].Z_KASSE_ID.Should().Be("TestCashbox");
            tseTransactions[0].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            tseTransactions[0].Z_NR.Should().Be(zNumber);
            tseTransactions[0].BON_ID.Should().Be("ftB04#IT3324");
            tseTransactions[0].TSE_ID.Should().Be(1);
            tseTransactions[0].TSE_TANR.Should().Be(3324);
            tseTransactions[0].TSE_TA_START.Should().Be(new DateTime(2021, 01, 19, 15, 21, 46));
            tseTransactions[0].TSE_TA_ENDE.Should().Be(new DateTime(2021, 01, 19, 15, 21, 46));
            tseTransactions[0].TSE_TA_VORGANGSART.Should().Be("Bestellung-V1");
            tseTransactions[0].TSE_TA_SIGZ.Should().Be(6626);
            tseTransactions[0].TSE_TA_SIG.Should().Be("u/Zm8xdnFD4AtEhbJLYUWrIdA057b4ccmIf0NS8v6+o8ZEchARzeFroZMjrk46efEBrYz4Saw9ARSUq71M8oDw==");
            tseTransactions[0].TSE_TA_FEHLER.Should().Be("");
            tseTransactions[0].TSE_VORGANGSDATEN.Should().Be("1;\"Bier 0,5 lt\";3.80\n1;\"Schnitzel\";9.20\n");
            tseTransactions[1].Z_KASSE_ID.Should().Be("TestCashbox");
            tseTransactions[1].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            tseTransactions[1].Z_NR.Should().Be(zNumber);
            tseTransactions[1].BON_ID.Should().Be("ftB05#IT3325");
            tseTransactions[1].TSE_ID.Should().Be(1);
            tseTransactions[1].TSE_TANR.Should().Be(3325);
            tseTransactions[1].TSE_TA_START.Should().Be(new DateTime(2021, 01, 19, 15, 40, 05));
            tseTransactions[1].TSE_TA_ENDE.Should().Be(new DateTime(2021, 01, 19, 15, 40, 05));
            tseTransactions[1].TSE_TA_VORGANGSART.Should().Be("Kassenbeleg-V1");
            tseTransactions[1].TSE_TA_SIGZ.Should().Be(6628);
            tseTransactions[1].TSE_TA_SIG.Should().Be("YZ5ey0a2ir3X8iTcA2i8lCipbKw/keQxWGttKUzWcAZQ5HvzUNU8Py0xvHwuPylDzv7PqLw9t5EJTZJjADNhrw==");
            tseTransactions[1].TSE_TA_FEHLER.Should().Be("");
            tseTransactions[1].TSE_VORGANGSDATEN.Should().Be("Beleg^3.80_9.20_0.00_0.00_0.00^13.00:Bar");

            var allocationGroups = ReadCsvEntries<BonkopfAbrKreis>(Path.Combine(targetDirectory, "allocation_groups.csv"));
            allocationGroups.Should().HaveCount(2);
            allocationGroups[0].Z_KASSE_ID.Should().Be("TestCashbox");
            allocationGroups[0].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            allocationGroups[0].Z_NR.Should().Be(zNumber);
            allocationGroups[0].BON_ID.Should().Be("ftB04#IT3324");
            allocationGroups[0].ABRECHNUNGSKREIS.Should().Be("R1");
            allocationGroups[1].Z_KASSE_ID.Should().Be("TestCashbox");
            allocationGroups[1].Z_ERSTELLUNG.Should().Be(dailyClosingMoment);
            allocationGroups[1].Z_NR.Should().Be(zNumber);
            allocationGroups[1].BON_ID.Should().Be("ftB05#IT3325");
            allocationGroups[1].ABRECHNUNGSKREIS.Should().Be("Tisch 1");

            Directory.Delete(targetDirectory, true);
        }

        private DSFinVKParameters GetModuleFormatterParameters() => new DSFinVKParameters
        {
            CashboxIdentification = "TestCashbox",
            TSECertificateBase64 = "MIICHTCCAcKgAwIBAgIhAPyXb2Ag4HWAVrKsSUvMAFvQzLuL80lwzovTn/K9bJYaMAoGCCqGSM49BAMCMF0xCzAJBgNVBAYTAkFUMQ8wDQYDVQQHDAZWaWVubmExFTATBgNVBAoMDGZpc2thbHkgR21iSDEmMCQGA1UEAwwdZmlza2FseSBzaWduIFRlc3QgQ2VydGlmaWNhdGUwHhcNMjAwODAzMTQyNzEzWhcNMzAwODAxMTQyNzEzWjBdMQswCQYDVQQGEwJBVDEPMA0GA1UEBwwGVmllbm5hMRUwEwYDVQQKDAxmaXNrYWx5IEdtYkgxJjAkBgNVBAMMHWZpc2thbHkgc2lnbiBUZXN0IENlcnRpZmljYXRlMFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE+rAm8qU9TzDGoJHW7jP0ZnlKiOrSpe4dr1tUHBInYVrySwjgtEM4gNC29OSjxYyJgEKhuxK/UN4L2bWvQzeD5KNTMFEwHQYDVR0OBBYEFO3mYd7qEtFMeatup/tqPBP/yKmzMB8GA1UdIwQYMBaAFO3mYd7qEtFMeatup/tqPBP/yKmzMA8GA1UdEwEB/wQFMAMBAf8wCgYIKoZIzj0EAwIDSQAwRgIhAMt1Dhgdec0WFFm3Z63D8YOEu50984LnlWyqYReEJvidAiEA1NJib9VaVX1PhxxcPMrMTJidgugfuKsBezxLZta+PyE="
        };

        private List<T> ReadCsvEntries<T>(string path)
        {
            using (var reader = new StreamReader(path))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Configuration.TypeConverterCache.AddConverter<bool>(new BoolTypeConverter());
                csv.Configuration.TypeConverterCache.AddConverter<DateTime>(new DateTimeTypeConverter());
                csv.Configuration.TypeConverterCache.AddConverter<decimal>(new DecimalTypeConverter());

                csv.Configuration.Delimiter = ";";

                return csv.GetRecords<T>().ToList();
            }
        }

        private static MasterDataConfiguration GetMasterDataConfiguration()
        {
            var fixture = new Fixture();
            var accountMasterData = fixture.Create<AccountMasterData>();
            var outletMasterData = fixture.Create<OutletMasterData>();
            var agencyMasterData = fixture.CreateMany<AgencyMasterData>();
            var posSystemMasterData = fixture.CreateMany<PosSystemMasterData>();
            return new MasterDataConfiguration
            {
                Account = accountMasterData,
                Outlet = outletMasterData,
                Agencies = agencyMasterData,
                PosSystems = posSystemMasterData
            };
        }
    }
}