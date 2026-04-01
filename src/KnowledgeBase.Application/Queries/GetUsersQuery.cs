namespace KnowledgeBase.Application.Queries;

public class GetUsersQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public bool? IsActive { get; set; }
    public bool? IsAdmin { get; set; }
    public DateTime? CreatedDate { get; set; }
    public UserSortBy SortBy { get; set; } = UserSortBy.CreatedDate;
    public SortDirection SortDirection { get; set; } = SortDirection.Desc;
}
