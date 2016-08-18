using System.Collections.Generic;

namespace WpfMultiselectTreeViewKit.Utils
{
    static class CollectionHelpers
    {
        public static void AddRange<T>(this IList<T> collection, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                collection.Add(item);
            }
        }
    }
}