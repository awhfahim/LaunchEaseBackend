using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Common.Domain.DataTransferObjects.Request;

public class DynamicQueryDto
{
    private int _size;
    private int _page = 1;

    [Required]
    [JsonPropertyName("size")]
    public int Size
    {
        get => _size;
        set => _size = value > 0 ? value : 10;
    }

    [Required]
    [JsonPropertyName("page")]
    public int Page
    {
        get => _page;
        set => _page = value > 0 ? value : 1;
    }

    [JsonPropertyName("filter")] public IReadOnlyList<DynamicFilterDto> Filters { get; init; } = [];
    [JsonPropertyName("sort")] public IReadOnlyList<DynamicSortingDto> Sorters { get; init; } = [];
}