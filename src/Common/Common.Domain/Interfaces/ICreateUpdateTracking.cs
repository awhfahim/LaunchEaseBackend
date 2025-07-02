namespace Common.Domain.Interfaces;

public interface ICreateUpdateTracking
{
    public long CreatedById { get; init; }
    public long? UpdatedById { get; set; }
}
