using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Ate.Engine.Serialization;

public static class ParameterValueNormalizer
{
    public static Dictionary<string, object> Normalize(Dictionary<string, object>? raw)
    {
        if (raw == null)
        {
            return new Dictionary<string, object>();
        }

        return raw.ToDictionary(k => k.Key, v => NormalizeValue(v.Value));
    }

    private static object NormalizeValue(object value)
    {
        if (value is JValue jValue)
        {
            return jValue.Value ?? string.Empty;
        }

        if (value is JObject jObject)
        {
            return jObject.Properties().ToDictionary(p => p.Name, p => NormalizeToken(p.Value));
        }

        if (value is JArray jArray)
        {
            return jArray.Select(NormalizeToken).ToList();
        }

        if (value is long l && l <= int.MaxValue && l >= int.MinValue)
        {
            return (int)l;
        }

        if (value is double d)
        {
            if (double.IsNaN(d) || double.IsInfinity(d))
            {
                throw new InvalidOperationException($"Type mismatch for parameter value '{d}': expected a finite numeric value.");
            }

            try
            {
                return Convert.ToDecimal(d, CultureInfo.InvariantCulture);
            }
            catch (OverflowException ex)
            {
                throw new InvalidOperationException($"Type mismatch for parameter value '{d}': numeric value is out of range for decimal.", ex);
            }
        }

        return value;
    }

    private static object NormalizeToken(JToken token)
    {
        return token switch
        {
            JValue jv => jv.Value ?? string.Empty,
            JObject jo => jo.Properties().ToDictionary(p => p.Name, p => NormalizeToken(p.Value)),
            JArray ja => ja.Select(NormalizeToken).ToList(),
            _ => token.ToString()
        };
    }
}
