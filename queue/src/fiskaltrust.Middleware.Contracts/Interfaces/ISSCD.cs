using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Contracts.Interfaces
{
    public interface ISSCD
    {
        public Task<bool> IsSSCDAvailable();
    }
}
