using System.Collections.Generic;
using System.Linq;

namespace fiskaltrust.Middleware.Localization.QueueAT.Extensions
{
    public static class ArrayExtensions
    {
        public static T[] Extend<T>(this T[] originalArray, T addItem) where T : class
        {
            var arr = new[] { addItem };
            return originalArray == null ? arr : originalArray.Concat(arr).ToArray();
        }

        public static T[] Extend<T>(this T[] originalArray, IEnumerable<T> addItems) where T : class
            => originalArray == null ? addItems.ToArray() : originalArray.Concat(addItems).ToArray();
    }
}
