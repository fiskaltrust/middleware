using System.Collections.Generic;
using fiskaltrust.ifPOS.v1.at;
using fiskaltrust.Middleware.Abstractions;

namespace fiskaltrust.Middleware.Localization.QueueAT.UnitTest.Helpers;

public class MockClientFactory : IClientFactory<IATSSCD>
{
    private readonly Dictionary<string, IATSSCD> _instances;
    public MockClientFactory(Dictionary<string, IATSSCD> instances)
    {
        _instances = instances;
    }

    public IATSSCD CreateClient(ClientConfiguration configuration)
    {
        return _instances[configuration.Url];
    }
}
