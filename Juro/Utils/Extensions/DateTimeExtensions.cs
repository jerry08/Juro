using System;

namespace AnimeDl.Utils.Extensions;

internal static class DateTimeExtensions
{
    private static readonly DateTime Jan1st1970 = new
        (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    //public static long CurrentTimeMillis()
    //{
    //    return (long)(DateTime.UtcNow - Jan1st1970).TotalMilliseconds;
    //}

    public static long ToUnixTimeMilliseconds(this DateTime dateTime)
    {
        return (long)(dateTime - Jan1st1970).TotalMilliseconds;
    }
}