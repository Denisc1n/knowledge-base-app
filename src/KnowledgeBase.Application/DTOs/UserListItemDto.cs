namespace KnowledgeBase.Application.DTOs;

public class UserListItemDto
{
    public string Name { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public bool Status { get; set; }
    public DateTime RegisteredAt { get; set; }
    public string Role { get; set; } = default!;
}
