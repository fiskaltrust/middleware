using System;
using System.Data;
using Dapper;

namespace fiskaltrust.Middleware.Storage.MySQL.TypeHandlers
{
    public class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
    {
        public override void SetValue(IDbDataParameter parameter, Guid value)
        {
            parameter.Value = value.ToString();
            parameter.DbType = DbType.String;
        }

        public override Guid Parse(object value) => Guid.Parse((string) value);
    }
}
