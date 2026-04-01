using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.Queries;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Infrastructure.Persistence;
using MongoDB.Driver;
using QuerySortDirection = KnowledgeBase.Application.Queries.SortDirection;

namespace KnowledgeBase.Infrastructure.Queries;

public class AdminUserReader : IAdminUserReader
{
    private readonly MongoContext _context;

    public AdminUserReader(MongoContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<UserListItemDto>> GetAllAsync(
        GetUsersQuery query,
        CancellationToken cancellationToken = default)
    {
        var skip = (query.Page - 1) * query.PageSize;
        var filter = BuildFilter(query);
        var sort = BuildSort(query.SortBy, query.SortDirection);

        return await _context.Users
            .Find(filter)
            .Sort(sort)
            .Project(x => new UserListItemDto
            {
                Name = x.FirstName,
                LastName = x.LastName,
                Email = x.Email,
                Status = x.IsActive,
                RegisteredAt = x.CreatedAtUtc
            })
            .Skip(skip)
            .Limit(query.PageSize)
            .ToListAsync(cancellationToken);
    }

    private static FilterDefinition<User> BuildFilter(GetUsersQuery query)
    {
        var filters = new List<FilterDefinition<User>>();

        if (query.IsActive.HasValue)
            filters.Add(Builders<User>.Filter.Eq(x => x.IsActive, query.IsActive.Value));

        if (query.IsAdmin.HasValue)
            filters.Add(Builders<User>.Filter.Eq(x => x.IsAdmin, query.IsAdmin.Value));

        if (query.CreatedDate.HasValue)
        {
            var start = query.CreatedDate.Value.Date;
            var end = start.AddDays(1);

            filters.Add(Builders<User>.Filter.Gte(x => x.CreatedAtUtc, start));
            filters.Add(Builders<User>.Filter.Lt(x => x.CreatedAtUtc, end));
        }

        return filters.Count == 0
            ? FilterDefinition<User>.Empty
            : Builders<User>.Filter.And(filters);
    }

    private static SortDefinition<User> BuildSort(UserSortBy sortBy, QuerySortDirection direction)
    {
        var sortBuilder = Builders<User>.Sort;

        return (sortBy, direction) switch
        {
            (UserSortBy.FirstName, QuerySortDirection.Asc) => sortBuilder
                .Ascending(x => x.FirstName)
                .Ascending(x => x.CreatedAtUtc)
                .Ascending(x => x.Id),
            (UserSortBy.FirstName, QuerySortDirection.Desc) => sortBuilder
                .Descending(x => x.FirstName)
                .Descending(x => x.CreatedAtUtc)
                .Descending(x => x.Id),
            (UserSortBy.LastName, QuerySortDirection.Asc) => sortBuilder
                .Ascending(x => x.LastName)
                .Ascending(x => x.CreatedAtUtc)
                .Ascending(x => x.Id),
            (UserSortBy.LastName, QuerySortDirection.Desc) => sortBuilder
                .Descending(x => x.LastName)
                .Descending(x => x.CreatedAtUtc)
                .Descending(x => x.Id),
            (UserSortBy.IsActive, QuerySortDirection.Asc) => sortBuilder
                .Ascending(x => x.IsActive)
                .Ascending(x => x.CreatedAtUtc)
                .Ascending(x => x.Id),
            (UserSortBy.IsActive, QuerySortDirection.Desc) => sortBuilder
                .Descending(x => x.IsActive)
                .Descending(x => x.CreatedAtUtc)
                .Descending(x => x.Id),
            (UserSortBy.IsAdmin, QuerySortDirection.Asc) => sortBuilder
                .Ascending(x => x.IsAdmin)
                .Ascending(x => x.CreatedAtUtc)
                .Ascending(x => x.Id),
            (UserSortBy.IsAdmin, QuerySortDirection.Desc) => sortBuilder
                .Descending(x => x.IsAdmin)
                .Descending(x => x.CreatedAtUtc)
                .Descending(x => x.Id),
            (UserSortBy.CreatedDate, QuerySortDirection.Asc) => sortBuilder
                .Ascending(x => x.CreatedAtUtc)
                .Ascending(x => x.Id),
            _ => sortBuilder
                .Descending(x => x.CreatedAtUtc)
                .Descending(x => x.Id)
        };
    }
}
