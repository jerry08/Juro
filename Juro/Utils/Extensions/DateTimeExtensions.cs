using System;

namespace Juro.Utils.Extensions;

internal static class DateTimeExtensions
{
#if NETCOREAPP || NETSTANDARD2_1
    private static readonly DateTime Jan1st1970 = DateTime.UnixEpoch;
#else
    private static readonly DateTime Jan1st1970 = new
        (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
#endif

    public static long CurrentTimeMillis()
    {
        return (long)(DateTime.UtcNow - Jan1st1970).TotalMilliseconds;
    }

    public static long CurrentTimeMillis(this DateTime date)
    {
        return (long)(date - Jan1st1970).TotalMilliseconds;
    }

    public static long ToUnixTimeMilliseconds(this DateTime dateTime)
    {
        return (long)(dateTime - Jan1st1970).TotalMilliseconds;
    }
}