using System.Text.RegularExpressions;

namespace LaunchEase.Api.Others;

internal class SlugifyParameterTransformer : IOutboundParameterTransformer
{
    public string? TransformOutbound(object? value)
    {
        if (value is not string strValue)
        {
            return null;
        }

        var regex = new Regex("([a-z])([A-Z])", RegexOptions.Compiled | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));
        return regex.Replace(strValue, "$1-$2").ToLowerInvariant();
    }
}