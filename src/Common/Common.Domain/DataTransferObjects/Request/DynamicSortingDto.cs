namespace Common.Domain.DataTransferObjects.Request;

public record DynamicSortingDto
{
    public DynamicSortingDto(string Field, string Dir)
    {
        this.Field = Field;
        this.Dir = Dir;
    }

    public string Field { get; init; }
    public string Dir { get; init; }

    public void Deconstruct(out string Field, out string Dir)
    {
        Field = this.Field;
        Dir = this.Dir;
    }
}