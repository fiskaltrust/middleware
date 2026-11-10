using System.Collections.Generic;

namespace fiskaltrust.Middleware.Journal.DE.Common.Helpers
{
    public static class AgencyIdProvider
    {
        private static readonly Dictionary<string, int> _ids = new Dictionary<string, int>();

        public static long GetIdByGuid(string agencyGuid)
        {
            if(string.IsNullOrEmpty(agencyGuid))
            {
                return 0;
            }

            if (_ids.TryGetValue(agencyGuid, out var id))
            {
                return id;
            }

            var nextId = _ids.Count + 1;
            _ids.Add(agencyGuid, nextId);
            return nextId;
        }
    }
}
