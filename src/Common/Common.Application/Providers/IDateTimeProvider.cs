namespace Common.Application.Providers;

public interface IDateTimeProvider
{
    DateTime CurrentUtcTime { get; }
    DateTime CurrentLocalTime { get; }
}