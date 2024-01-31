using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Juro.Core.Utils.Extensions;

internal static class StringExtensions
{
    public static int? ToIntOrNull(this string? value) => int.TryParse(value, out var i) ? i : null;

    public static int? ToIntOrNull(this string? value, int radix)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        var length = value?.Length;
        if (length == 0)
            return null;

        return Convert.ToByte(value, radix);
    }

    public static string Reverse(this string value)
    {
        var charArray = value.ToCharArray();
        Array.Reverse(charArray);
        return new string(charArray);
    }

    public static string FindBetween(this string value, string a, string b)
    {
        var start = value.IndexOf(a);
        if (start != -1)
        {
            start += a.Length;

            var end = value.IndexOf(b, start);
            if (end != -1)
            {
                return value.Substring(start, end - start);
            }
        }

        return string.Empty;
    }

    public static string SubstringAfter(this string value, string a)
    {
        var start = value.IndexOf(a);
        if (start != -1)
        {
            start += a.Length;
            return value.Substring(start);
        }

        return string.Empty;
    }

    public static string SubstringBefore(this string value, string stopAt)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            var charLocation = value.IndexOf(stopAt, StringComparison.Ordinal);

            if (charLocation > 0)
            {
                return value.Substring(0, charLocation);
            }
        }

        return string.Empty;
    }

    public static string DecodeBase64(this string value) =>
        Encoding.UTF8.GetString(Convert.FromBase64String(value));

    public static byte[] DecodeBase64ToBytes(this string value) => Convert.FromBase64String(value);

    private static readonly Regex _whitespace = new(@"\s+");

    public static string ReplaceWhitespaces(this string input, string replacement) =>
        _whitespace.Replace(input, replacement);

    public static string RemoveWhitespaces(this string input) =>
        input.ReplaceWhitespaces(string.Empty);

    /// <summary>
    /// If the input string starts with the given prefix, returns a substring
    /// with the prefix removed. Otherwise, returns the original string.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="prefix"></param>
    /// <returns></returns>
    public static string RemovePrefix(this string input, string prefix)
    {
        if (input.StartsWith(prefix))
            return input.Remove(0, prefix.Length);

        return input;
    }

    public static string RemovePrefix(this string input, int prefixLen)
    {
        if (input.Length < prefixLen)
            return string.Empty;

        return input.Remove(0, prefixLen);
    }
}
