using System.Runtime.InteropServices;

namespace AWSSDK.SQS.Extensions.Extensions;

internal static class IEnumerableExtensions
{
    public static IEnumerable<List<T>> Split<T>(this List<T> items, int nSize)
    {
        for (var i = 0; i < items.Count; i += nSize)
            yield return items.GetRange(i, Math.Min(nSize, items.Count - i));
    }
}
