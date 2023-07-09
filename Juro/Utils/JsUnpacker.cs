using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Juro.Utils.Extensions;

namespace Juro.Utils
{
    /// <summary>
    /// This singleton class provides functionality to detect and unpack packed
    /// javascript based on Dean Edwards JavaScript's Packer.
    /// See <see href="http://dean.edwards.name/packer/">[Dean Edwards JavaScript's Packer]</see>
    /// </summary>
    public static class JsUnpacker
    {
        /// <summary>
        /// Regex to detect packed functions.
        /// </summary>
        private static readonly Regex _packedRegex = new(
            "eval[(]function[(]p,a,c,k,e,[r|d]?",
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );

        /// <summary>
        /// Regex to get and group the packed javascript.
        /// Needed to get information and unpack the code.
        /// </summary>
        private static readonly Regex _packedExtractRegex = new(
            "[}][(]'(.*)', *(\\d+), *(\\d+), *'(.*?)'[.]split[(]'[|]'[)]",
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );

        /// <summary>
        /// Matches function names and variables to de-obfuscate the code.
        /// </summary>
        private static readonly Regex _unpackReplaceRegex = new(
            "\\b\\w+\\b",
            RegexOptions.IgnoreCase | RegexOptions.Multiline
        );

        /// <summary>
        /// Check if script is packed.
        /// </summary>
        /// <param name="scriptBlock">The string to check if it is packed.</param>
        /// <returns>Whether the [scriptBlock] contains packed code or not.</returns>
        public static bool IsPacked(string? scriptBlock)
        {
            if (string.IsNullOrWhiteSpace(scriptBlock))
            {
                return false;
            }

            return _packedRegex.IsMatch(scriptBlock);
        }

        public static List<string?> GetPackedScripts(IEnumerable<string?> scriptBlocks)
        {
            var list = new List<string?>();

            foreach (var script in scriptBlocks)
            {
                if (script is null)
                {
                    list.Add(null);
                    continue;
                }

                if (_packedRegex.IsMatch(script))
                {
                    list.Add(_packedRegex.Match(script).Value);
                }
            }

            return list;
        }

        /// <summary>
        /// Unpack the passed [scriptBlock].
        /// It matches all found occurrences and returns them as separate Strings in a list.
        /// </summary>
        /// <param name="scriptBlock">The String to unpack.</param>
        /// <returns>Unpacked code in a list or an empty list if non is packed.</returns>
        public static List<string?> Unpack(string? scriptBlock)
        {
            if (!IsPacked(scriptBlock))
            {
                return new List<string?>();
            }

            return Unpacking(scriptBlock);
        }

        /// <summary>
        /// Unpack the passed [scriptBlock].
        /// It matches all found occurrences and combines them into a single String.
        /// </summary>
        /// <param name="scriptBlock">The String to unpack.</param>
        /// <returns>Unpacked code in a list combined by a whitespace to a single String.</returns>
        public static string UnpackAndCombine(string? scriptBlock)
        {
            var unpacked = Unpack(scriptBlock);
            return string.Join(" ", unpacked);
        }

        /// <summary>
        /// Unpacking functionality.
        /// Match all found occurrences, get the information group and unbase it.
        /// If found symtabs are more or less than the count provided in code, the occurrence will be ignored
        /// because it cannot be unpacked correctly.
        /// </summary>
        /// <param name="scriptBlock">The String to unpack.</param>
        /// <returns>A list of all unpacked code from all found packed and unpackable occurrences found.</returns>
        private static List<string?> Unpacking(string? scriptBlock)
        {
            if (scriptBlock is null)
            {
                return new List<string?>();
            }

            var matches = _packedExtractRegex.Matches(scriptBlock).OfType<Match>();

            var list = new List<string?>();

            foreach (var match in matches)
            {
                var payload = match.Groups[1]?.Value;
                var symtab = match.Groups[4]?.Value?.Split('|');
                var radix = match.Groups[2]?.Value?.ToIntOrNull() ?? 10;
                var count = match.Groups[3]?.Value?.ToIntOrNull();
                var unbaser = new Unbaser(radix);

                if (symtab is null || count is null || symtab.Length != count)
                {
                    list.Add(null);
                    continue;
                }

                if (payload is not null)
                {
                    payload = _unpackReplaceRegex.Replace(
                        payload,
                        (Match match) =>
                        {
                            var word = match.Value;
                            var unbased = symtab[unbaser.Unbase(word)];
                            if (string.IsNullOrEmpty(unbased))
                            {
                                return word;
                            }

                            return unbased;
                        }
                    );
                }

                list.Add(payload);
            }

            return list;
        }
    }
}