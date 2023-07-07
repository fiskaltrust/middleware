using fiskaltrust.ifPOS.v1;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Contracts.Extensions
{
    public enum ftStateType
    {
        Information,
        Warning,
        Error
    }

    public static class ReceiptResponseExtensions
    {
        public static void SetFtStateData(this ReceiptResponse receiptResponse, ftStateType stateType, string text)
        {
            switch (stateType)
            {
                case ftStateType.Information:
                {
                    receiptResponse.ftStateData = JsonConvert.SerializeObject(new { Information = text });
                    break;
                }
                case ftStateType.Warning:
                {
                    receiptResponse.ftStateData = JsonConvert.SerializeObject(new { Warning = text });
                    break;
                }
                case ftStateType.Error:
                {
                    receiptResponse.ftStateData = JsonConvert.SerializeObject(new { Error = text });
                    break;
                }
            }
        }
   }
}
