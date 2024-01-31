namespace Juro.Core.Utils.Extensions;

internal static class ArrayExtensions
{
    public static T[] CopyOfRange<T>(this T[] source, int fromIndex, int toIndex)
    {
        var len = toIndex - fromIndex;
        var dest = new T[len];

        for (var i = 0; i < len; i++)
            dest[i] = source[fromIndex + i];

        return dest;
    }
}
