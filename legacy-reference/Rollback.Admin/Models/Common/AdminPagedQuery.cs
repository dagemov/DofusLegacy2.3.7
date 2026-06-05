namespace Rollback.Admin.Models.Common;

public sealed class AdminPagedQuery
{
    public string Search { get; set; } = string.Empty;

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 25;

    public string? SortBy { get; set; }

    public bool Descending { get; set; }
}
