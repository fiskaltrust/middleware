namespace fiskaltrust.Middleware.PostFiscalization;

/// <summary>Thrown at queue startup when an eInvoicing or eReporting section is present but unusable.</summary>
public class PostFiscalizationConfigurationException : Exception
{
    public PostFiscalizationConfigurationException(string message) : base(message) { }

    public PostFiscalizationConfigurationException(string message, Exception innerException) : base(message, innerException) { }
}
