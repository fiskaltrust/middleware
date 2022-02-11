using System;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Models.Dto;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Communication.Helpers
{
    public static class ErrorHelper
    {
        public static string GetErrorType(string responseContent)
        {
            try
            {
                return JsonConvert.DeserializeObject<ErrorDto>(responseContent)?.ErrorType;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
