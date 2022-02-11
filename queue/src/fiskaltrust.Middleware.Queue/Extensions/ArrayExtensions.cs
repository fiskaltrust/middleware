using System.Linq;

namespace fiskaltrust.Middleware.Queue.Extensions
{
    public static class ArrayExtensions
    {
        public static T[] Concat<T>(this T[] array, T element) => array.Concat(new[] { element }).ToArray();
    }
}
