using System;
using System.Collections.Generic;

namespace fiskaltrust.Middleware.Journal.DE.Common.Helpers
{
    //In rare cases some bin files contain 0x0 bytes which cause issues in some processing steps. This class is eliminates those bytes for specified queues.
    public class Eliminater0x0
    {
        private static readonly HashSet<Guid> queuesWith0x0 = new HashSet<Guid>() { 
            Guid.Parse("fb8f9080-5c56-474d-a8a6-bf4749092ad7") ,
            Guid.Parse("4d8e932c-bb36-482a-9d4c-664dbf4e04f9"),
            Guid.Parse("934a2680-63ef-4785-9a14-800014ebd1cf")

        };

        public static string Eliminate0x0IfNeeded(Guid queueId, string text)
        {
            if (queuesWith0x0.Contains(queueId))
            {
                text = text.Replace(Convert.ToChar(0x0).ToString(), "");
            }
            return text;
        }

    }
}
