namespace Common.Application.Providers;

public interface IStringTransformationProvider
{
    string ToTitleCase(string input);
    string ToSentenceCase(string input);
}
