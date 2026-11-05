using System.Collections.Generic;

namespace fiskaltrust.Middleware.Journal.DE.Common.Helpers
{
    public static class TseIdProvider
    {
        private static readonly Dictionary<string, int> _ids = new Dictionary<string, int>();

        public static int GetIdByPublicKey(string publicKey)
        {
            var key = publicKey ?? string.Empty;
            if (_ids.TryGetValue(key, out var id))
            {
                return id;
            }

            var nextId = _ids.Count + 1;
            _ids.Add(key, nextId);
            return nextId;
        }
    }
}
