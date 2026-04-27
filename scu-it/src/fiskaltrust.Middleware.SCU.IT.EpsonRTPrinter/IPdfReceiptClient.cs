using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.Models;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter;

public interface IPdfReceiptClient
{
    Task<GetPdfResponse?> GetReceiptPdfAsync(string cashBoxId, string znum, string numdoc, string matricola, string date);
}
