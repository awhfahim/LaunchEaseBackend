namespace Common.Domain.Interfaces;

public interface ITimestamp
{
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; set; }
}