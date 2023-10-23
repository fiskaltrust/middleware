using System;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT.Dummy.Services
{
    // The IXXSSCDProvider interface defines the contract for managing the Signature Creation Units (SCUs) for the specific market "XX" (replace with the actual market name).
    // Implementations of this interface should provide thread-safe access to SCU instances, allowing registration and retrieval for the given market.
    public interface IXXSSCDProvider
    {
        // Use the IXXSSCD of the new market instead of object as the type of Instance
        // Gets the current SCU instance for the market "XX" (replace with the actual market name).
        public object Instance { get; }
        
        Task RegisterCurrentScuAsync();
    }
}