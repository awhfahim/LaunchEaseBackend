namespace Common.Domain.DataTransferObjects.Response;

public record PagedData<T>
{
    public PagedData(IEnumerable<T> Payload, long TotalCount)
    {
        this.Payload = Payload;
        this.TotalCount = TotalCount;
    }

    public static PagedData<T> CreateDefault()
    {
        return new PagedData<T>([], 0);
    }

    public IEnumerable<T> Payload { get; init; }
    public long TotalCount { get; init; }
};