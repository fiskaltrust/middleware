using System;
using Xunit;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueFR.Models;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Localization.QueueFR.Extensions;

namespace fiskaltrust.Middleware.Localization.QueueFR.UnitTest.Extensions
{
    public class QueueFRExtensionsTests
    {
        [Fact]
        public void AddReceiptTotalsToTicketTotals_Should_AddTotalsToTicketTotals()
        {
            // Arrange
            var queueFr = new ftQueueFR();
            var totals = new Totals
            {
                Totalizer = 10,
                CITotalNormal = 5,
                CITotalReduced1 = 3,
                CITotalReduced2 = 2,
                CITotalReducedS = 1,
                CITotalZero = 0,
                CITotalUnknown = 4,
                PITotalCash = 7,
                PITotalNonCash = 8,
                PITotalInternal = 6,
                PITotalUnknown = 9
            };

            // Act
            queueFr.AddReceiptTotalsToTicketTotals(totals);

            // Assert
            Assert.Equal(10, queueFr.TTotalizer);
            Assert.Equal(5, queueFr.TCITotalNormal);
            Assert.Equal(3, queueFr.TCITotalReduced1);
            Assert.Equal(2, queueFr.TCITotalReduced2);
            Assert.Equal(1, queueFr.TCITotalReducedS);
            Assert.Equal(0, queueFr.TCITotalZero);
            Assert.Equal(4, queueFr.TCITotalUnknown);
            Assert.Equal(7, queueFr.TPITotalCash);
            Assert.Equal(8, queueFr.TPITotalNonCash);
            Assert.Equal(6, queueFr.TPITotalInternal);
            Assert.Equal(9, queueFr.TPITotalUnknown);
        }

        [Fact]
        public void AddReceiptTotalsToInvoiceTotals_Should_AddTotalsToQueueFR()
        {
            // Arrange
            var queueFr = new ftQueueFR();
            var totals = new Totals()
            {
                Totalizer = 10,
                CITotalNormal = 5,
                CITotalReduced1 = 3,
                CITotalReduced2 = 2,
                CITotalReducedS = 1,
                CITotalZero = 5,
                CITotalUnknown = 4,
                PITotalCash = 7,
                PITotalNonCash = 8,
                PITotalInternal = 6,
                PITotalUnknown = 9,
            };

            // Act
            queueFr.AddReceiptTotalsToInvoiceTotals(totals);

            // Assert
            Assert.Equal(10, queueFr.ITotalizer);
            Assert.Equal(5, queueFr.ICITotalNormal);
            Assert.Equal(3, queueFr.ICITotalReduced1);
            Assert.Equal(2, queueFr.ICITotalReduced2);
            Assert.Equal(1, queueFr.ICITotalReducedS);
            Assert.Equal(5, queueFr.ICITotalZero);
            Assert.Equal(4, queueFr.ICITotalUnknown);
            Assert.Equal(7, queueFr.IPITotalCash);
            Assert.Equal(8, queueFr.IPITotalNonCash);
            Assert.Equal(6, queueFr.IPITotalInternal);
            Assert.Equal(9, queueFr.IPITotalUnknown);

        }

        [Fact]
        public void ResetShiftTotalizer_ShouldResetTotalizerAndCITotals()
        {
            // Arrange
            var queueFr = new ftQueueFR();
            var queueItem = new ftQueueItem();

            queueFr.GShiftTotalizer = 100;
            queueFr.GShiftCITotalNormal = 50;
            queueFr.GShiftCITotalReduced1 = 25;
            queueFr.GShiftCITotalReduced2 = 10;
            queueFr.GShiftCITotalReducedS = 5;
            queueFr.GShiftCITotalZero = 0;
            queueFr.GShiftCITotalUnknown = 15;
            queueFr.GShiftPITotalCash = 75;
            queueFr.GShiftPITotalNonCash = 30;
            queueFr.GShiftPITotalInternal = 20;
            queueFr.GShiftPITotalUnknown = 5;

            // Act
            queueFr.ResetShiftTotalizer(queueItem);

            // Assert
            Assert.Equal(0, queueFr.GShiftTotalizer);
            Assert.Equal(0, queueFr.GShiftCITotalNormal);
            Assert.Equal(0, queueFr.GShiftCITotalReduced1);
            Assert.Equal(0, queueFr.GShiftCITotalReduced2);
            Assert.Equal(0, queueFr.GShiftCITotalReducedS);
            Assert.Equal(0, queueFr.GShiftCITotalZero);
            Assert.Equal(0, queueFr.GShiftCITotalUnknown);
            Assert.Equal(0, queueFr.GShiftPITotalCash);
            Assert.Equal(0, queueFr.GShiftPITotalNonCash);
            Assert.Equal(0, queueFr.GShiftPITotalInternal);
            Assert.Equal(0, queueFr.GShiftPITotalUnknown);
            Assert.Equal(queueItem.ftQueueItemId, queueFr.GLastShiftQueueItemId);
        }

        [Fact]
        public void ResetMonthlyTotalizers_ShouldResetMonthlyTotalizersAndSetYearTotalizers()
        {
            // Arrange
            var queueFr = new ftQueueFR();
            var queueItem = new ftQueueItem();

            queueFr.GMonthTotalizer = 100;
            queueFr.GMonthCITotalNormal = 10;
            queueFr.GMonthCITotalReduced1 = 20;
            queueFr.GMonthCITotalReduced2 = 30;
            queueFr.GMonthCITotalReducedS = 40;
            queueFr.GMonthCITotalZero = 50;
            queueFr.GMonthCITotalUnknown = 60;
            queueFr.GMonthPITotalCash = 70;
            queueFr.GMonthPITotalNonCash = 80;
            queueFr.GMonthPITotalInternal = 90;
            queueFr.GMonthPITotalUnknown = 100;

            // Act
            queueFr.ResetMonthlyTotalizers(queueItem);

            // Assert
            Assert.Equal(100, queueFr.GYearTotalizer);
            Assert.Equal(10, queueFr.GYearCITotalNormal);
            Assert.Equal(20, queueFr.GYearCITotalReduced1);
            Assert.Equal(30, queueFr.GYearCITotalReduced2);
            Assert.Equal(40, queueFr.GYearCITotalReducedS);
            Assert.Equal(50, queueFr.GYearCITotalZero);
            Assert.Equal(60, queueFr.GYearCITotalUnknown);
            Assert.Equal(70, queueFr.GYearPITotalCash);
            Assert.Equal(80, queueFr.GYearPITotalNonCash);
            Assert.Equal(90, queueFr.GYearPITotalInternal);
            Assert.Equal(100, queueFr.GYearPITotalUnknown);

            Assert.Equal(0, queueFr.GMonthTotalizer);
            Assert.Equal(0, queueFr.GMonthCITotalNormal);
            Assert.Equal(0, queueFr.GMonthCITotalReduced1);
            Assert.Equal(0, queueFr.GMonthCITotalReduced2);
            Assert.Equal(0, queueFr.GMonthCITotalReducedS);
            Assert.Equal(0, queueFr.GMonthCITotalZero);
            Assert.Equal(0, queueFr.GMonthCITotalUnknown);
            Assert.Equal(0, queueFr.GMonthPITotalCash);
            Assert.Equal(0, queueFr.GMonthPITotalNonCash);
            Assert.Equal(0, queueFr.GMonthPITotalInternal);
            Assert.Equal(0, queueFr.GMonthPITotalUnknown);

            Assert.Equal(queueItem.ftQueueItemId, queueFr.GLastMonthQueueItemId);
        }

        [Fact]
        public void ResetDailyTotalizers_ShouldResetDayTotalsAndAddToMonthTotals()
        {
            // Arrange
            var queueFr = new ftQueueFR();
            var queueItem = new ftQueueItem();

            queueFr.GDayTotalizer = 100;
            queueFr.GDayCITotalNormal = 10;
            queueFr.GDayCITotalReduced1 = 20;
            queueFr.GDayCITotalReduced2 = 30;
            queueFr.GDayCITotalReducedS = 40;
            queueFr.GDayCITotalZero = 50;
            queueFr.GDayCITotalUnknown = 60;
            queueFr.GDayPITotalCash = 70;
            queueFr.GDayPITotalNonCash = 80;
            queueFr.GDayPITotalInternal = 90;
            queueFr.GDayPITotalUnknown = 100;

            // Act
            queueFr.ResetDailyTotalizers(queueItem);

            // Assert
            Assert.Equal(100, queueFr.GMonthTotalizer);
            Assert.Equal(10, queueFr.GMonthCITotalNormal);
            Assert.Equal(20, queueFr.GMonthCITotalReduced1);
            Assert.Equal(30, queueFr.GMonthCITotalReduced2);
            Assert.Equal(40, queueFr.GMonthCITotalReducedS);
            Assert.Equal(50, queueFr.GMonthCITotalZero);
            Assert.Equal(60, queueFr.GMonthCITotalUnknown);
            Assert.Equal(70, queueFr.GMonthPITotalCash);
            Assert.Equal(80, queueFr.GMonthPITotalNonCash);
            Assert.Equal(90, queueFr.GMonthPITotalInternal);
            Assert.Equal(100, queueFr.GMonthPITotalUnknown);

            Assert.Equal(0, queueFr.GDayTotalizer);
            Assert.Equal(0, queueFr.GDayCITotalNormal);
            Assert.Equal(0, queueFr.GDayCITotalReduced1);
            Assert.Equal(0, queueFr.GDayCITotalReduced2);
            Assert.Equal(0, queueFr.GDayCITotalReducedS);
            Assert.Equal(0, queueFr.GDayCITotalZero);
            Assert.Equal(0, queueFr.GDayCITotalUnknown);
            Assert.Equal(0, queueFr.GDayPITotalCash);
            Assert.Equal(0, queueFr.GDayPITotalNonCash);
            Assert.Equal(0, queueFr.GDayPITotalInternal);
            Assert.Equal(0, queueFr.GDayPITotalUnknown);

            Assert.Equal(queueItem.ftQueueItemId, queueFr.GLastDayQueueItemId);
        }

        [Fact]
        public void ResetDailyTotalizers_ShouldResetDailyTotalizers()
        {
            // Arrange
            var queueFr = new ftQueueFR();
            var queueItem = new ftQueueItem();

            queueFr.GMonthTotalizer = 100;
            queueFr.GMonthCITotalNormal = 200;
            queueFr.GMonthCITotalReduced1 = 300;
            queueFr.GMonthCITotalReduced2 = 400;
            queueFr.GMonthCITotalReducedS = 500;
            queueFr.GMonthCITotalZero = 600;
            queueFr.GMonthCITotalUnknown = 700;
            queueFr.GMonthPITotalCash = 800;
            queueFr.GMonthPITotalNonCash = 900;
            queueFr.GMonthPITotalInternal = 1000;
            queueFr.GMonthPITotalUnknown = 1100;

            queueFr.GDayTotalizer = 50;
            queueFr.GDayCITotalNormal = 60;
            queueFr.GDayCITotalReduced1 = 70;
            queueFr.GDayCITotalReduced2 = 80;
            queueFr.GDayCITotalReducedS = 90;
            queueFr.GDayCITotalZero = 100;
            queueFr.GDayCITotalUnknown = 110;
            queueFr.GDayPITotalCash = 120;
            queueFr.GDayPITotalNonCash = 130;
            queueFr.GDayPITotalInternal = 140;
            queueFr.GDayPITotalUnknown = 150;

            // Act
            queueFr.ResetDailyTotalizers(queueItem);

            // Assert
            Assert.Equal(150, queueFr.GMonthTotalizer);
            Assert.Equal(260, queueFr.GMonthCITotalNormal);
            Assert.Equal(370, queueFr.GMonthCITotalReduced1);
            Assert.Equal(480, queueFr.GMonthCITotalReduced2);
            Assert.Equal(590, queueFr.GMonthCITotalReducedS);
            Assert.Equal(700, queueFr.GMonthCITotalZero);
            Assert.Equal(810, queueFr.GMonthCITotalUnknown);
            Assert.Equal(920, queueFr.GMonthPITotalCash);
            Assert.Equal(1030, queueFr.GMonthPITotalNonCash);
            Assert.Equal(1140, queueFr.GMonthPITotalInternal);
            Assert.Equal(1250, queueFr.GMonthPITotalUnknown);

            Assert.Equal(0, queueFr.GDayTotalizer);
            Assert.Equal(0, queueFr.GDayCITotalNormal);
            Assert.Equal(0, queueFr.GDayCITotalReduced1);
            Assert.Equal(0, queueFr.GDayCITotalReduced2);
            Assert.Equal(0, queueFr.GDayCITotalReducedS);
            Assert.Equal(0, queueFr.GDayCITotalZero);
            Assert.Equal(0, queueFr.GDayCITotalUnknown);
            Assert.Equal(0, queueFr.GDayPITotalCash);
            Assert.Equal(0, queueFr.GDayPITotalNonCash);
            Assert.Equal(0, queueFr.GDayPITotalInternal);
            Assert.Equal(0, queueFr.GDayPITotalUnknown);

            Assert.Equal(queueItem.ftQueueItemId, queueFr.GLastDayQueueItemId);
        }

        [Fact]
        public void ResetArchiveTotalizers_ShouldResetTotalizers()
        {
            // Arrange
            var queueFr = new ftQueueFR();
            var queueItem = new ftQueueItem();

            queueFr.ATotalizer = 100;
            queueFr.ACITotalNormal = 200;
            queueFr.ACITotalReduced1 = 300;
            queueFr.ACITotalReduced2 = 400;
            queueFr.ACITotalReducedS = 500;
            queueFr.ACITotalZero = 600;
            queueFr.ACITotalUnknown = 700;
            queueFr.APITotalCash = 800;
            queueFr.APITotalNonCash = 900;
            queueFr.APITotalInternal = 1000;
            queueFr.APITotalUnknown = 1100;

            // Act
            queueFr.ResetArchiveTotalizers(queueItem);

            // Assert
            Assert.Equal(0, queueFr.ATotalizer);
            Assert.Equal(0, queueFr.ACITotalNormal);
            Assert.Equal(0, queueFr.ACITotalReduced1);
            Assert.Equal(0, queueFr.ACITotalReduced2);
            Assert.Equal(0, queueFr.ACITotalReducedS);
            Assert.Equal(0, queueFr.ACITotalZero);
            Assert.Equal(0, queueFr.ACITotalUnknown);
            Assert.Equal(0, queueFr.APITotalCash);
            Assert.Equal(0, queueFr.APITotalNonCash);
            Assert.Equal(0, queueFr.APITotalInternal);
            Assert.Equal(0, queueFr.APITotalUnknown);
            Assert.Equal(queueItem.ftQueueItemId, queueFr.ALastQueueItemId);
        }

        [Fact]
        public void GetShiftTotals_ShouldReturnTotals()
        {
            // Arrange
            var queueFr = new ftQueueFR()
            {
                GShiftTotalizer = 100,
                GShiftCITotalNormal = 10,
                GShiftCITotalReduced1 = 20,
                GShiftCITotalReduced2 = 30,
                GShiftCITotalReducedS = 40,
                GShiftCITotalZero = 50,
                GShiftCITotalUnknown = 60,
                GShiftPITotalCash = 70,
                GShiftPITotalNonCash = 80,
                GShiftPITotalInternal = 90,
                GShiftPITotalUnknown = 100
            };

            // Act
            var totals = queueFr.GetShiftTotals();

            // Assert
            Assert.Equal(100, totals.Totalizer);
            Assert.Equal(10, totals.CITotalNormal);
            Assert.Equal(20, totals.CITotalReduced1);
            Assert.Equal(30, totals.CITotalReduced2);
            Assert.Equal(40, totals.CITotalReducedS);
            Assert.Equal(50, totals.CITotalZero);
            Assert.Equal(60, totals.CITotalUnknown);
            Assert.Equal(70, totals.PITotalCash);
            Assert.Equal(80, totals.PITotalNonCash);
            Assert.Equal(90, totals.PITotalInternal);
            Assert.Equal(100, totals.PITotalUnknown);
        }

        [Fact]
        public void GetYearTotals_ShouldReturnTotals()
        {
            // Arrange
            var queueFr = new ftQueueFR()
            {
                GYearTotalizer = 100,
                GYearCITotalNormal = 200,
                GYearCITotalReduced1 = 300,
                GYearCITotalReduced2 = 400,
                GYearCITotalReducedS = 500,
                GYearCITotalZero = 600,
                GYearCITotalUnknown = 700,
                GYearPITotalCash = 800,
                GYearPITotalNonCash = 900,
                GYearPITotalInternal = 1000,
                GYearPITotalUnknown = 1100
            };

            // Act
            var totals = queueFr.GetYearTotals();

            // Assert
            Assert.Equal(100, totals.Totalizer);
            Assert.Equal(200, totals.CITotalNormal);
            Assert.Equal(300, totals.CITotalReduced1);
            Assert.Equal(400, totals.CITotalReduced2);
            Assert.Equal(500, totals.CITotalReducedS);
            Assert.Equal(600, totals.CITotalZero);
            Assert.Equal(700, totals.CITotalUnknown);
            Assert.Equal(800, totals.PITotalCash);
            Assert.Equal(900, totals.PITotalNonCash);
            Assert.Equal(1000, totals.PITotalInternal);
            Assert.Equal(1100, totals.PITotalUnknown);
        }
        [Fact]
        public void GetMonthTotals_ReturnsTotals()
        {
            // Arrange
            var queueFr = new ftQueueFR()
            {
                GMonthTotalizer = 100,
                GMonthCITotalNormal = 10,
                GMonthCITotalReduced1 = 20,
                GMonthCITotalReduced2 = 30,
                GMonthCITotalReducedS = 40,
                GMonthCITotalZero = 50,
                GMonthCITotalUnknown = 60,
                GMonthPITotalCash = 70,
                GMonthPITotalNonCash = 80,
                GMonthPITotalInternal = 90,
                GMonthPITotalUnknown = 100
            };

            // Act
            var result = queueFr.GetMonthTotals();

            // Assert
            Assert.Equal(100, result.Totalizer);
            Assert.Equal(10, result.CITotalNormal);
            Assert.Equal(20, result.CITotalReduced1);
            Assert.Equal(30, result.CITotalReduced2);
            Assert.Equal(40, result.CITotalReducedS);
            Assert.Equal(50, result.CITotalZero);
            Assert.Equal(60, result.CITotalUnknown);
            Assert.Equal(70, result.PITotalCash);
            Assert.Equal(80, result.PITotalNonCash);
            Assert.Equal(90, result.PITotalInternal);
            Assert.Equal(100, result.PITotalUnknown);
        }

        [Fact]
        public void GetDayTotals_ShouldReturnCorrectTotals()
        {
            // Arrange
            var queueFr = new ftQueueFR()
            {
                GDayTotalizer = 100,
                GDayCITotalNormal = 10,
                GDayCITotalReduced1 = 20,
                GDayCITotalReduced2 = 30,
                GDayCITotalReducedS = 40,
                GDayCITotalZero = 50,
                GDayCITotalUnknown = 60,
                GDayPITotalCash = 70,
                GDayPITotalNonCash = 80,
                GDayPITotalInternal = 90,
                GDayPITotalUnknown = 100
            };

            // Act
            var totals = queueFr.GetDayTotals();

            // Assert
            Assert.Equal(100, totals.Totalizer);
            Assert.Equal(10, totals.CITotalNormal);
            Assert.Equal(20, totals.CITotalReduced1);
            Assert.Equal(30, totals.CITotalReduced2);
            Assert.Equal(40, totals.CITotalReducedS);
            Assert.Equal(50, totals.CITotalZero);
            Assert.Equal(60, totals.CITotalUnknown);
            Assert.Equal(70, totals.PITotalCash);
            Assert.Equal(80, totals.PITotalNonCash);
            Assert.Equal(90, totals.PITotalInternal);
            Assert.Equal(100, totals.PITotalUnknown);
        }

        [Fact]
        public void GetArchiveTotals_Should_Return_Totals_From_ftQueueFR()
        {
            // Arrange
            var queueFr = new ftQueueFR()
            {
                ATotalizer = 100,
                ACITotalNormal = 10,
                ACITotalReduced1 = 20,
                ACITotalReduced2 = 30,
                ACITotalReducedS = 40,
                ACITotalZero = 50,
                ACITotalUnknown = 60,
                APITotalCash = 70,
                APITotalNonCash = 80,
                APITotalInternal = 90,
                APITotalUnknown = 100
            };

            // Act
            var totals = queueFr.GetArchiveTotals();

            // Assert
            Assert.Equal(100, totals.Totalizer);
            Assert.Equal(10, totals.CITotalNormal);
            Assert.Equal(20, totals.CITotalReduced1);
            Assert.Equal(30, totals.CITotalReduced2);
            Assert.Equal(40, totals.CITotalReducedS);
            Assert.Equal(50, totals.CITotalZero);
            Assert.Equal(60, totals.CITotalUnknown);
            Assert.Equal(70, totals.PITotalCash);
            Assert.Equal(80, totals.PITotalNonCash);
            Assert.Equal(90, totals.PITotalInternal);
            Assert.Equal(100, totals.PITotalUnknown);
        }
    }
}
