using System;
using System.Data;
using Dapper;

namespace fiskaltrust.Middleware.Storage.MySQL.TypeHandlers
{
    public class MySQLDateTimeTypeHandler : SqlMapper.TypeHandler<DateTime>
    {
        public override void SetValue(IDbDataParameter parameter, DateTime value)
        {
            parameter.Value = value.Ticks;
            parameter.DbType = DbType.Int64;
        }

        public override DateTime Parse(object value) => new DateTime((long) value);
    }
}
