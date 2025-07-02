using System.Text.RegularExpressions;

namespace Acm.Infrastructure.Misc;

public static class PhcFormatter
{
    private static readonly Regex IdRegex = new("^[a-z0-9-]{1,32}$", RegexOptions.Compiled);
    private static readonly Regex NameRegex = new("^[a-z0-9-]{1,32}$", RegexOptions.Compiled);
    private static readonly Regex VersionRegex = new(@"^v=(\d+)$", RegexOptions.Compiled);

    public static string Serialize(string id, int? version, Dictionary<string, object> parameters,
        byte[] salt,
        byte[] hash)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        if (!IdRegex.IsMatch(id))
        {
            throw new ArgumentException("Invalid id format");
        }

        var fields = new List<string> { "", id };

        // Optional version
        if (version.HasValue)
        {
            if (version < 0)
            {
                throw new ArgumentException("Version must be a positive integer");
            }

            fields.Add($"v={version}");
        }

        // Convert parameters to name=value format

        if (parameters.Keys.Any(key => !NameRegex.IsMatch(key)))
        {
            throw new ArgumentException("Invalid parameter name format");
        }

        foreach (var key in parameters.Keys)
        {
            if (parameters[key] is int)
            {
                var str = parameters[key].ToString();
                ArgumentNullException.ThrowIfNull(str);
                parameters[key] = str;
            }
            else if (parameters[key] is byte[] buffer)
            {
                parameters[key] = Convert.ToBase64String(buffer).TrimEnd('=');
            }
        }

        var paramString = ObjectToKeyValue(parameters);
        fields.Add(paramString);


        // Salt and hash encoding
        fields.Add(Convert.ToBase64String(salt).TrimEnd('='));
        fields.Add(Convert.ToBase64String(hash).TrimEnd('='));
        return string.Join("$", fields);
    }


    public static (string id, int? version, Dictionary<string, string>? parameters, byte[] salt, byte[] hash)
        Deserialize(string phcString)
    {
        if (string.IsNullOrEmpty(phcString) || !phcString.StartsWith('$'))
        {
            throw new ArgumentException("Invalid PHC string format");
        }

        var fields = phcString.Split('$', StringSplitOptions.RemoveEmptyEntries);
        var index = 0;

        // Extract ID
        var id = fields[index++];
        if (!IdRegex.IsMatch(id))
        {
            throw new ArgumentException("Invalid id format");
        }

        int? version = null;
        if (VersionRegex.IsMatch(fields[index]))
        {
            version = int.Parse(VersionRegex.Match(fields[index++]).Groups[1].Value);
        }

        Dictionary<string, string>? parameters = null;
        if (fields.Length > index && fields[index].Contains('='))
        {
            parameters = KeyValueToObject(fields[index++]);
        }

        var salt = Array.Empty<byte>();
        var hash = Array.Empty<byte>();

        if (fields.Length > index)
        {
            salt = Convert.FromBase64String(AddBase64Padding(fields[index++]));
        }

        if (fields.Length > index)
        {
            hash = Convert.FromBase64String(AddBase64Padding(fields[index]));
        }

        return (id, version, parameters, salt, hash);
    }

// Helper method to add padding to Base64 strings
    private static string AddBase64Padding(string base64)
    {
        var paddingNeeded = (4 - base64.Length % 4) % 4;
        return base64 + new string('=', paddingNeeded);
    }

    private static Dictionary<string, string> KeyValueToObject(string str)
    {
        var parameters = new Dictionary<string, string>();
        foreach (var pair in str.Split(','))
        {
            var keyValue = pair.Split('=');
            if (keyValue.Length != 2)
            {
                throw new FormatException("params must be in the format name=value");
            }

            parameters[keyValue[0]] = keyValue[1];
        }

        return parameters;
    }

    private static string ObjectToKeyValue(Dictionary<string, object> parameters)
    {
        var keyValuePairs = parameters.Keys.Select(key => $"{key}={parameters[key]}").ToList();
        return string.Join(",", keyValuePairs);
    }
}

