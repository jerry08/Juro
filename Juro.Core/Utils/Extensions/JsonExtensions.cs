using System;
using System.Diagnostics;
using System.Text.Json.Nodes;

namespace Juro.Core.Utils.Extensions;

public static class JsonExtensions
{
    public static bool IsValidJson(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        value = value.Trim();
        if (
            (value.StartsWith("{") && value.EndsWith("}"))
            || (value.StartsWith("[") && value.EndsWith("]"))
        )
        {
            try
            {
                var obj = JsonNode.Parse(value);
                return obj is not null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return false;
            }
        }
        else
        {
            return false;
        }
    }
}
