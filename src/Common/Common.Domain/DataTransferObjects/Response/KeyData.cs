namespace Common.Domain.DataTransferObjects.Response;

public record KeyData<TKey, TData>
{
    public required TKey Key { get; init; }
    public required TData Data { get; init; }
}