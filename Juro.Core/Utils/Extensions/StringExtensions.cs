using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Juro.Core.Utils.Extensions;

internal static class StringExtensions
{
    public static int? ToIntOrNull(this string? value) => int.TryParse(value, out var i) ? i : null;

    private static readonly int[] _digits =
    [
        0,
        1,
        2,
        3,
        4,
        5,
        6,
        7,
        8,
        9,
        -1,
        -1,
        -1,
        -1,
        -1,
        -1,
        -1,
        10,
        11,
        12,
        13,
        14,
        15,
        16,
        17,
        18,
        19,
        20,
        21,
        22,
        23,
        24,
        25,
        26,
        27,
        28,
        29,
        30,
        31,
        32,
        33,
        34,
        35,
        -1,
        -1,
        -1,
        -1,
        -1,
        -1,
        10,
        11,
        12,
        13,
        14,
        15,
        16,
        17,
        18,
        19,
        20,
        21,
        22,
        23,
        24,
        25,
        26,
        27,
        28,
        29,
        30,
        31,
        32,
        33,
        34,
        35
    ];

    internal static int DigitOf(char c, int radix)
    {
        int result;

        if (c >= '0' && c <= 'z')
            result = _digits[c - '0'];
        else if (c < '\u0080')
            result = -1;
        else if (c >= '\uFF21' && c <= '\uFF3A') // full-width latin capital letter
            result = c - '\uFF21' + 10;
        else if (c >= '\uFF41' && c <= '\uFF5A') // full-width latin small letter
            result = c - '\uFF41' + 10;
        else
            result = c;

        if (result >= radix)
            return -1;
        else
            return result;
    }

    public static int? ToIntOrNull(this string? value, int radix)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        var length = value?.Length;
        if (length == 0)
            return null;

        int start;
        bool isNegative;
        int limit;

        var firstChar = value![0];
        if (firstChar < '0')
        {
            // Possible leading sign
            if (length == 1)
                return null; // non-digit (possible sign) only, no digits after

            start = 1;

            if (firstChar == '-')
            {
                isNegative = true;
                limit = int.MinValue;
            }
            else if (firstChar == '+')
            {
                isNegative = false;
                limit = -int.MaxValue;
            }
            else
            {
                return null;
            }
        }
        else
        {
            start = 0;
            isNegative = false;
            limit = -int.MaxValue;
        }

        var limitForMaxRadix = (-int.MaxValue) / 36;

        var limitBeforeMul = limitForMaxRadix;
        var result = 0;

        for (var i = start; i < length; i++)
        {
            var digit = DigitOf(value[i], radix);

            if (digit < 0)
                return null;
            if (result < limitBeforeMul)
            {
                if (limitBeforeMul == limitForMaxRadix)
                {
                    limitBeforeMul = limit / radix;

                    if (result < limitBeforeMul)
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }

            result *= radix;

            if (result < limit + digit)
                return null;

            result -= digit;
        }

        return isNegative ? result : -result;
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
