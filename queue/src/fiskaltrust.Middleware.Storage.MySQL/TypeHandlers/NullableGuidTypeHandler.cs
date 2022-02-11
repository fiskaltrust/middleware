using System;
using System.Data;
using Dapper;

namespace fiskaltrust.Middleware.Storage.MySQL.TypeHandlers
{
    public class NullableGuidTypeHandler : SqlMapper.TypeHandler<Guid?>
    {
        public override void SetValue(IDbDataParameter parameter, Guid? value)
        {
            parameter.Value = value.ToString();
            parameter.DbType = DbType.String;
        }

        public override Guid? Parse(object value)
        {
            if(value == null)
            {
                return null;
            }
            else
            {
                return Guid.Parse((string) value);
            }
        }
    }
}
