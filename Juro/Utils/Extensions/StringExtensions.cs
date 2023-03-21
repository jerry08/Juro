using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Juro.Utils.Extensions;

internal static class StringExtensions
{
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

    public static string SubstringBefore(this string text, string stopAt)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            var charLocation = text.IndexOf(stopAt, StringComparison.Ordinal);

            if (charLocation > 0)
            {
                return text.Substring(0, charLocation);
            }
        }

        return string.Empty;
    }

    public static string ReplaceInvalidChars(this string fileName)
    {
        return string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
    }

    //? - any character (one and only one)
    //* - any characters (zero or more)
    public static string WildCardToRegular(string value)
    {
        // If you want to implement both "*" and "?"
        return "^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$";

        // If you want to implement "*" only
        //return "^" + Regex.Escape(value).Replace("\\*", ".*") + "$";
    }

    public static string RemoveBadChars1(this string name)
    {
        //string result = new string(name.Where(c => char.IsLetter(c) || c == '\'').ToArray());
        var result = new string(name.Where(c => char.IsLetter(c) || c == ' ').ToArray());
        return result.Replace(' ', '-');
    }

    public static string RemoveBadChars(string word)
    {
        //Regex reg = new Regex("[^a-zA-Z']");
        var reg = new Regex("[^a-zA-Z' ]"); //Don't replace spaces
        var regString = reg.Replace(word, string.Empty);

        return regString.Replace(' ', '-');
    }

    static void Test()
    {
        var test = "Some Data X";

        var endsWithEx = Regex.IsMatch(test, WildCardToRegular("*X"));
        var startsWithS = Regex.IsMatch(test, WildCardToRegular("S*"));
        var containsD = Regex.IsMatch(test, WildCardToRegular("*D*"));

        // Starts with S, ends with X, contains "me" and "a" (in that order) 
        var complex = Regex.IsMatch(test, WildCardToRegular("S*me*a*X"));
    }

    public static IEnumerable<string> SplitInParts(this string s, int partLength)
    {
        if (s == null)
            throw new ArgumentNullException(nameof(s));
        if (partLength <= 0)
            throw new ArgumentException("Part length has to be positive.", nameof(partLength));

        for (var i = 0; i < s.Length; i += partLength)
            yield return s.Substring(i, Math.Min(partLength, s.Length - i));
    }
}