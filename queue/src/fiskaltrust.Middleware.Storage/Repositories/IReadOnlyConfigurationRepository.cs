
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Storage.ES;
using fiskaltrust.Middleware.Storage.EU;

namespace fiskaltrust.Middleware.Storage;

public interface IReadOnlyConfigurationRepository
{
    public Task<IEnumerable<ftSignaturCreationUnitES>> GetSignaturCreationUnitESListAsync();

    public Task<ftSignaturCreationUnitES> GetSignaturCreationUnitESAsync(Guid signaturCreationUnitESId);


    public Task<IEnumerable<ftQueueES>> GetQueueESListAsync();
    public Task<IEnumerable<ftQueueEU>> GetQueueEUListAsync();

    public Task<ftQueueEU> GetQueueEUAsync(Guid queueEUId);

}