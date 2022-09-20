using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;

namespace fiskaltrust.Middleware.Storage.EF.Helpers
{
    public static class JsonExtensions
    {
        [DbFunction("CodeFirstDatabaseSchema", "JSON_VALUE")]
        public static string JsonValue(string column, string path)
        {
            throw new NotSupportedException();
        }

        public class RegisterJsonValueFunctionConvention : IStoreModelConvention<EdmModel>
        {
            public void Apply(EdmModel item, DbModel model)
            {
                var expressionParameter = FunctionParameter.Create("expression", GetStorePrimitiveType(model, PrimitiveTypeKind.String), ParameterMode.In);
                var pathParameter = FunctionParameter.Create("path", GetStorePrimitiveType(model, PrimitiveTypeKind.String), ParameterMode.In);
                var returnValue = FunctionParameter.Create("result", GetStorePrimitiveType(model, PrimitiveTypeKind.String), ParameterMode.ReturnValue);
                CreateAndAddFunction(item, "JSON_VALUE", new[] { expressionParameter, pathParameter }, new[] { returnValue });
            }

            protected EdmFunction CreateAndAddFunction(EdmModel item, string name, IList<FunctionParameter> parameters, IList<FunctionParameter> returnValues)
            {
                var payload = new EdmFunctionPayload { StoreFunctionName = name, Parameters = parameters, ReturnParameters = returnValues, Schema = GetDefaultSchema(item), IsBuiltIn = true };
                var function = EdmFunction.Create(name, GetDefaultNamespace(item), item.DataSpace, payload, null);
                item.AddItem(function);
                return function;
            }

            protected EdmType GetStorePrimitiveType(DbModel model, PrimitiveTypeKind typeKind)
            {
                return model.ProviderManifest.GetStoreType(TypeUsage.CreateDefaultTypeUsage(PrimitiveType.GetEdmPrimitiveType(typeKind))).EdmType;
            }

            protected string GetDefaultNamespace(EdmModel layerModel)
            {
                return layerModel.GlobalItems.OfType<EdmType>().Select(t => t.NamespaceName).Distinct().Single();
            }

            protected string GetDefaultSchema(EdmModel layerModel)
            {
                return layerModel.Container.EntitySets.Select(s => s.Schema).Distinct().SingleOrDefault();
            }
        }
    }
}
