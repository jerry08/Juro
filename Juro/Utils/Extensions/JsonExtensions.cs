using System;
using System.Text.Json.Nodes;

namespace Juro.Utils.Extensions
{
    public class JsonExtensions
    {
        public static bool IsValidJson(string strInput)
        {
            if (string.IsNullOrWhiteSpace(strInput))
            {
                return false;
            }

            strInput = strInput.Trim();
            if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || //For object
                (strInput.StartsWith("[") && strInput.EndsWith("]"))) //For array
            {
                try
                {
                    var obj = JsonNode.Parse(strInput);
                    return obj is not null;
                }
                catch (Exception ex) //some other exception
                {
#if DEBUG
                    Console.WriteLine(ex.ToString());
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
#endif
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }
}