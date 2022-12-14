using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Juro.Utils.Extensions;

public class JsonExtensions
{
    public static bool IsValidJson(string strInput)
    {
        if (string.IsNullOrWhiteSpace(strInput))
            return false;
        strInput = strInput.Trim();
        if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || //For object
            (strInput.StartsWith("[") && strInput.EndsWith("]"))) //For array
        {
            try
            {
                var obj = JToken.Parse(strInput);
                return true;
            }
            catch (JsonReaderException jex)
            {
#if DEBUG
                //Exception in parsing json
                Console.WriteLine(jex.Message);
                System.Diagnostics.Debug.WriteLine(jex.Message);
#endif
                return false;
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