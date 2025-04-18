
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Storage.ES;

public interface IReadOnlyConfigurationRepository
{
    Task<IEnumerable<ftSignaturCreationUnitES>> GetSignaturCreationUnitESListAsync();

    Task<ftSignaturCreationUnitES> GetSignaturCreationUnitESAsync(Guid signaturCreationUnitESId);


    Task<IEnumerable<ftQueueES>> GetQueueESListAsync();

    Task<ftQueueES> GetQueueESAsync(Guid queueESId);

}