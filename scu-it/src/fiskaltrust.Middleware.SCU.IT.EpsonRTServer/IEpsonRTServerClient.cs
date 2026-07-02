using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.IT.EpsonRTServer.Models;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTServer
{
    public interface IEpsonRTServerClient
    {
        /// <summary>Requests a Token for a till (initialises the CCDC blockchain). fpserver.cgi / createToken.</summary>
        Task<RtServerResponse> CreateTokenAsync(string tillId);

        /// <summary>Programs the complete till map on the RT Server. fpserver.cgi / createTills.</summary>
        Task<RtServerResponse> CreateTillsAsync(string userId, string password, IEnumerable<string> tillIds);

        /// <summary>Emits a fiscal receipt. The full &lt;createReceipt&gt; body (incl. hash chain) is built by the mapping.</summary>
        Task<RtServerResponse> CreateReceiptAsync(string createReceiptXml);

        /// <summary>Sends a daily closure request for a till. fpserver.cgi / createDailyClosure.</summary>
        Task<RtServerResponse> CreateDailyClosureAsync(string tillId, int closureType);

        /// <summary>Reads RT Server status information. fpserver.cgi / createReport / serverInfo.</summary>
        Task<RtServerResponse> GetServerInfoAsync();

        /// <summary>Reads the RT Server date/time and UTC offset. fpserver.cgi / createReport / serverTime.</summary>
        Task<RtServerResponse> GetServerTimeAsync();

        /// <summary>Reads the firmware and fpserver.cgi versions. fpserver.cgi / createReport / firmwareVersion.</summary>
        Task<RtServerResponse> GetFirmwareVersionAsync();

        /// <summary>Reads the current-day totals for a till. fpserver.cgi / createReport / fiscalInformation.</summary>
        Task<RtServerResponse> GetFiscalInformationAsync(string tillId);

        /// <summary>Retrieves the RT Server public certificate key (instant lottery). fpserver.cgi / createReport / publicKey.</summary>
        Task<RtServerResponse> GetPublicKeyAsync();

        /// <summary>Requests an RT Server Z Report (not a till one). fpmate.cgi / printZReport.</summary>
        Task<RtServerResponse> PrintServerZReportAsync();

        /// <summary>Reboots the on-board web server (used after a till map change). fpmate.cgi / rebootWebServer.</summary>
        Task<RtServerResponse> RebootWebServerAsync();
    }
}
