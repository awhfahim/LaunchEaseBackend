using Common.Application.Providers;
using Humanizer;

namespace Common.Infrastructure.Providers;

public class StringTransformationProvider: IStringTransformationProvider
{
    public string ToTitleCase(string input)
    {
        return input.Humanize().Titleize().Trim();
    }

    public string ToSentenceCase(string input)
    {
        return input.Humanize().Transform(To.SentenceCase).Trim();
    }
}
