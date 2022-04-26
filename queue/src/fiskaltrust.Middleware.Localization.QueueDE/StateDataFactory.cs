using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Localization.QueueDE.MasterData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace fiskaltrust.Middleware.Localization.QueueDE
{
    public class StateDataFactory
    {
        public static async Task<string> AppendTseInfoAsync(IDESSCD client, string currentStateDataJson = null)
        {
            var tseInfo = await client.GetTseInfoAsync().ConfigureAwait(false);

            var currentStateData = ParseCurrentStateData(currentStateDataJson);
            currentStateData.Add("TseInfo", JToken.FromObject(tseInfo));

            return JsonConvert.SerializeObject(currentStateData);
        }

        public static string ApendOpenTransactionState(IEnumerable<OpenTransaction> openTransactions, string currentStateDataJson = null)
        {
            var transactionNumberDictionary = openTransactions.Select(t => new KeyValuePair<string, long>(t.cbReceiptReference, t.TransactionNumber)).ToDictionary(i => i.Key);

            var currentStateData = ParseCurrentStateData(currentStateDataJson);
            currentStateData.Add("TransactionNumberDictionary", JToken.FromObject(transactionNumberDictionary));

            return JsonConvert.SerializeObject(currentStateData);
        }

        public static async Task<string> AppendMasterDataAsync(IMasterDataService masterDataService, string currentStateDataJson = null)
        {
            var masterData = await masterDataService.GetCurrentDataAsync().ConfigureAwait(false);

            if (masterData == null)
            {
                return currentStateDataJson;
            }

            var currentStateData = ParseCurrentStateData(currentStateDataJson);
            currentStateData.Add("MasterData", JToken.FromObject(masterData));

            return JsonConvert.SerializeObject(currentStateData);
        }

        public static string AppendDailyClosingNumber(int dailyClosingnumber, string currentStateDataJson = null)
        {
            var currentStateData = ParseCurrentStateData(currentStateDataJson);
            currentStateData.Add("DailyClosingNumber", JToken.FromObject(dailyClosingnumber));

            return JsonConvert.SerializeObject(currentStateData);
        }

        private static JObject ParseCurrentStateData(string currentStateDataJson)
        {
            var currentStateData = new JObject();
            if (!string.IsNullOrEmpty(currentStateDataJson))
            {
                try
                {
                    currentStateData = (JObject) JsonConvert.DeserializeObject(currentStateDataJson);
                }
                catch (Exception) { }
            }

            return currentStateData;
        }
    }
}
