namespace RecruitmentPlatform.Application.DTOs.Common;

/// <summary>
/// Base query parameters for paged, searchable, sortable list endpoints.
/// </summary>
public class PaginationQuery
{
    private const int MaxPageSize = 100;
    private int _pageSize = 10;
    private int _page = 1;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value is < 1 or > MaxPageSize ? (value > MaxPageSize ? MaxPageSize : 10) : value;
    }

    public string? Search { get; set; }

    public string? SortBy { get; set; }

    public bool SortDescending { get; set; }
}
