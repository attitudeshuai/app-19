namespace LeftoverShare.API.DTOs.Common;

public class PagedRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; } = "desc";
}
