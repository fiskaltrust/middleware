using System.Data;
using Dapper;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Storage.SQLite.TypeHandlers
{
    public class NumeratorStorageTypeHandler : SqlMapper.TypeHandler<NumeratorStorage>
    {
        public override void SetValue(IDbDataParameter parameter, NumeratorStorage value)
        {
            parameter.Value = value == null ? null : JsonConvert.SerializeObject(value);
            parameter.DbType = DbType.String;
        }

        public override NumeratorStorage Parse(object value) => value is string json ? JsonConvert.DeserializeObject<NumeratorStorage>(json) : null;
    }
}
