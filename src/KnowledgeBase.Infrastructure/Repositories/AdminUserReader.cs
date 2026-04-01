using KnowledgeBase.Application.Abstractions;
using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Infrastructure.Persistence;
using MongoDB.Driver;

namespace KnowledgeBase.Infrastructure.Repositories;

public class AdminUserReader : IAdminUserReader
{
    private readonly MongoContext _context;

    public AdminUserReader(MongoContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<UserListItemDto>> GetAllAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var skip = (page - 1) * pageSize;

        return await _context.Users
            .Find(FilterDefinition<User>.Empty)
            .Project(x => new UserListItemDto
            {
                Name = x.FirstName,
                LastName = x.LastName,
                Email = x.Email,
                Status = x.IsActive,
                RegisteredAt = x.CreatedAtUtc
            })
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);
    }
}
