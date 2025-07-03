using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Common.HttpApi.Others;

public record PaginationQueryParameter
{
    private int _limit = 10;
    private int _page = 0;

    [BindRequired, FromQuery, JsonPropertyName("page")]
    public int Page
    {
        get => _page;
        set => _page = value > 0 ? value : 0;
    }

    [BindRequired, FromQuery, JsonPropertyName("limit")]
    public int Limit
    {
        get => _limit;
        set => _limit = value > 0 ? value : 10;
    }
}
