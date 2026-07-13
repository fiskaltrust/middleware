using System;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter;

public sealed class EpsonNoResponseException : TaskCanceledException
{
    public EpsonNoResponseException(string message) : base(message)
    {
    }

    public EpsonNoResponseException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
