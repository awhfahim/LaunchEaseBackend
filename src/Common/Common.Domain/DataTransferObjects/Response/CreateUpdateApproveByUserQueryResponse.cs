namespace Common.Domain.DataTransferObjects.Response;

public record CreateUpdateApproveByUserQueryResponse<TEntity>
{
    public required TEntity Entity { get; set; }
    public required long CreatedById { get; init; }
    public required string CreatedByUserName { get; init; }
    public required long? UpdatedById { get; init; }
    public required string? UpdatedByName { get; init; }
}
