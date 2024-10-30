using System;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Storage.ES;

namespace fiskaltrust.Middleware.Storage.Repositories;


public interface IReadOnlyConfigurationRepository : storage.V0.IReadOnlyConfigurationRepository
{
    Task<ftSignaturCreationUnitES> GetSignaturCreationUnitESAsync(Guid signaturCreationUnitESId);
}

public interface IConfigurationRepository : storage.V0.IConfigurationRepository, IReadOnlyConfigurationRepository
{
    Task InsertOrUpdateSignaturCreationUnitESAsync(ftSignaturCreationUnitES scu);
}