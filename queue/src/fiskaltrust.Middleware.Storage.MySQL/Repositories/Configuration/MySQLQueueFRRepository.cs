using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.MySQL.Repositories.Configuration
{
    public class MySQLQueueFRRepository : AbstractMySQLRepository<Guid, ftQueueFR>, IConfigurationItemRepository<ftQueueFR>
    {
        public MySQLQueueFRRepository(string connectionString) : base(connectionString) { }

        public override void EntityUpdated(ftQueueFR entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        public override async Task<ftQueueFR> GetAsync(Guid id)
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryFirstOrDefaultAsync<ftQueueFR>("Select * from ftQueueFR where ftQueueFRId = @QueueFRId", new { QueueFRId = id }).ConfigureAwait(false);
            }
        }

        public override async Task<IEnumerable<ftQueueFR>> GetAsync()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                return await connection.QueryAsync<ftQueueFR>("select * from ftQueueFR").ConfigureAwait(false);
            }
        }

        public async Task InsertOrUpdateAsync(ftQueueFR entity)
        {
            EntityUpdated(entity);
            var sql = "REPLACE INTO ftQueueFR " +
                      "(ALastMoment, GLastDayMoment, GLastMonthMoment, GLastShiftMoment, GLastYearMoment, MessageMoment, UsedFailedMomentMax, UsedFailedMomentMin, ACITotalNormal, ACITotalReduced1, ACITotalReduced2, ACITotalReducedS, ACITotalUnknown, ACITotalZero, APITotalCash, APITotalInternal, APITotalNonCash, APITotalUnknown, ATotalizer, BCITotalNormal, BCITotalReduced1, BCITotalReduced2, BCITotalReducedS, BCITotalUnknown, BCITotalZero, BPITotalCash, BPITotalInternal, BPITotalNonCash, BPITotalUnknown, BTotalizer, CTotalizer, GDayCITotalNormal, GDayCITotalReduced1, GDayCITotalReduced2, GDayCITotalReducedS, GDayCITotalUnknown, GDayCITotalZero, GDayPITotalCash, GDayPITotalInternal, GDayPITotalNonCash, GDayPITotalUnknown, GDayTotalizer, GMonthCITotalNormal, GMonthCITotalReduced1, GMonthCITotalReduced2, GMonthCITotalReducedS, GMonthCITotalUnknown, GMonthCITotalZero, GMonthPITotalCash, GMonthPITotalInternal, GMonthPITotalNonCash, GMonthPITotalUnknown, GMonthTotalizer, GShiftCITotalNormal, GShiftCITotalReduced1, GShiftCITotalReduced2, GShiftCITotalReducedS, GShiftCITotalUnknown, GShiftCITotalZero, GShiftPITotalCash, GShiftPITotalInternal, GShiftPITotalNonCash, GShiftPITotalUnknown, GShiftTotalizer, GYearCITotalNormal, GYearCITotalReduced1, GYearCITotalReduced2, GYearCITotalReducedS, GYearCITotalUnknown, GYearCITotalZero, GYearPITotalCash, GYearPITotalInternal, GYearPITotalNonCash, GYearPITotalUnknown, GYearTotalizer, ICITotalNormal, ICITotalReduced1, ICITotalReduced2, ICITotalReducedS, ICITotalUnknown, ICITotalZero, IPITotalCash, IPITotalInternal, IPITotalNonCash, IPITotalUnknown, ITotalizer, PPITotalCash, PPITotalInternal, PPITotalNonCash, PPITotalUnknown, PTotalizer, TCITotalNormal, TCITotalReduced1, TCITotalReduced2, TCITotalReducedS, TCITotalUnknown, TCITotalZero, TPITotalCash, TPITotalInternal, TPITotalNonCash, TPITotalUnknown, TTotalizer, XTotalizer, ftQueueFRId, ftSignaturCreationUnitFRId, ALastQueueItemId, GLastDayQueueItemId, GLastMonthQueueItemId, GLastShiftQueueItemId, GLastYearQueueItemId, UsedFailedQueueItemId, MessageCount, UsedFailedCount, ANumerator, BNumerator, CNumerator, GNumerator, INumerator, LNumerator, PNumerator, TNumerator, XNumerator, ALastHash, BLastHash, CashBoxIdentification, CLastHash, GLastHash, ILastHash, LLastHash, PLastHash, Siret, TLastHash, XLastHash,TimeStamp) " +
                      "Values (@ALastMoment, @GLastDayMoment, @GLastMonthMoment, @GLastShiftMoment, @GLastYearMoment, @MessageMoment, @UsedFailedMomentMax, @UsedFailedMomentMin, @ACITotalNormal, @ACITotalReduced1, @ACITotalReduced2, @ACITotalReducedS, @ACITotalUnknown, @ACITotalZero, @APITotalCash, @APITotalInternal, @APITotalNonCash, @APITotalUnknown, @ATotalizer, @BCITotalNormal, @BCITotalReduced1, @BCITotalReduced2, @BCITotalReducedS, @BCITotalUnknown, @BCITotalZero, @BPITotalCash, @BPITotalInternal, @BPITotalNonCash, @BPITotalUnknown, @BTotalizer, @CTotalizer, @GDayCITotalNormal, @GDayCITotalReduced1, @GDayCITotalReduced2, @GDayCITotalReducedS, @GDayCITotalUnknown, @GDayCITotalZero, @GDayPITotalCash, @GDayPITotalInternal, @GDayPITotalNonCash, @GDayPITotalUnknown, @GDayTotalizer, @GMonthCITotalNormal, @GMonthCITotalReduced1, @GMonthCITotalReduced2, @GMonthCITotalReducedS, @GMonthCITotalUnknown, @GMonthCITotalZero, @GMonthPITotalCash, @GMonthPITotalInternal, @GMonthPITotalNonCash, @GMonthPITotalUnknown, @GMonthTotalizer, @GShiftCITotalNormal, @GShiftCITotalReduced1, @GShiftCITotalReduced2, @GShiftCITotalReducedS, @GShiftCITotalUnknown, @GShiftCITotalZero, @GShiftPITotalCash, @GShiftPITotalInternal, @GShiftPITotalNonCash, @GShiftPITotalUnknown, @GShiftTotalizer, @GYearCITotalNormal, @GYearCITotalReduced1, @GYearCITotalReduced2, @GYearCITotalReducedS, @GYearCITotalUnknown, @GYearCITotalZero, @GYearPITotalCash, @GYearPITotalInternal, @GYearPITotalNonCash, @GYearPITotalUnknown, @GYearTotalizer, @ICITotalNormal, @ICITotalReduced1, @ICITotalReduced2, @ICITotalReducedS, @ICITotalUnknown, @ICITotalZero, @IPITotalCash, @IPITotalInternal, @IPITotalNonCash, @IPITotalUnknown, @ITotalizer, @PPITotalCash, @PPITotalInternal, @PPITotalNonCash, @PPITotalUnknown, @PTotalizer, @TCITotalNormal, @TCITotalReduced1, @TCITotalReduced2, @TCITotalReducedS, @TCITotalUnknown, @TCITotalZero, @TPITotalCash, @TPITotalInternal, @TPITotalNonCash, @TPITotalUnknown, @TTotalizer, @XTotalizer, @ftQueueFRId, @ftSignaturCreationUnitFRId, @ALastQueueItemId, @GLastDayQueueItemId, @GLastMonthQueueItemId, @GLastShiftQueueItemId, @GLastYearQueueItemId, @UsedFailedQueueItemId, @MessageCount, @UsedFailedCount, @ANumerator, @BNumerator, @CNumerator, @GNumerator, @INumerator, @LNumerator, @PNumerator, @TNumerator, @XNumerator, @ALastHash, @BLastHash, @CashBoxIdentification, @CLastHash, @GLastHash, @ILastHash, @LLastHash, @PLastHash, @Siret, @TLastHash, @XLastHash,@TimeStamp);";
            using (var connection = new MySqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await connection.ExecuteAsync(sql, entity).ConfigureAwait(false);
            }
        }

        protected override Guid GetIdForEntity(ftQueueFR entity) => entity.ftQueueFRId;
    }
}
