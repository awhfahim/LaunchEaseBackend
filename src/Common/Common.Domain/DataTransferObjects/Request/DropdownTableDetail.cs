namespace Common.Domain.DataTransferObjects.Request;

public record DropdownTableDetail
{
    public required string SchemaName { get; init; }
    public required string TableName { get; init; }
    public required string IdColumn { get; init; }
    public required string NameColumn { get; init; }
    public IList<(string table, string firstColumn, string secondColumn)>? Joins { get; set; }
    public IList<(string column, object value)>? Filters { get; set; }
}
