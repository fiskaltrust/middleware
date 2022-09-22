using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace fiskaltrust.Middleware.Storage.EFCore.Helpers
{
    public static class JsonExtensions
    {
        [DbFunction("JSON_VALUE", IsBuiltIn = true, Schema = "")]
        public static string JsonValue(string column, [NotParameterized] string path)
        {
            throw new NotSupportedException();
        }
    }
}
