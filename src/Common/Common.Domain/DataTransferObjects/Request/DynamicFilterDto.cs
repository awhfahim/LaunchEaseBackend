namespace Common.Domain.DataTransferObjects.Request;

public record DynamicFilterDto
{
    public DynamicFilterDto(string Field, string Type, string Value)
    {
        this.Field = Field;
        this.Type = Type;
        this.Value = Value;
    }
        
    public string Field { get; init; }
    public string Type { get; init; }
    public string Value { get; init; }
}