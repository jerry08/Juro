using System;
using System.Collections.Generic;
using System.Linq;
using Juro.Core.Utils.Extensions;

namespace Juro.Core.Utils;

internal class Unbaser(int @base)
{
    private readonly int _base = @base;

    private readonly Dictionary<int, string> _alphabet = new()
    {
        [52] = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOP",
        [54] = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQR",
        [62] = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ",
        [95] =
            " !\\\"#\\$%&\\\\'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\\\]^_`abcdefghijklmnopqrstuvwxyz{|}~",
    };

    public int Unbase(string value)
    {
        if (_base is >= 2 and <= 36)
            return value.ToIntOrNull(_base) ?? 0;

        var selector = _base switch
        {
            > 62 => 95,
            > 54 => 62,
            > 52 => 54,
            _ => 52,
        };

        var dict = _alphabet[selector]?.ToCharArray();

        var returnVal = 0;

        var valArray = value.ToCharArray().AsEnumerable().Reverse().ToArray();
        for (var i = 0; i < valArray.Length; i++)
        {
            var cipher = valArray[i];
            returnVal += (int)(Math.Pow(_base, i) * (dict?[cipher] ?? 0));
        }

        return returnVal;
    }
}
