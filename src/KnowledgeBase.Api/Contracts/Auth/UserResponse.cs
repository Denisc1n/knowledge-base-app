namespace KnowledgeBase.Api.Contracts.Auth;

public class UserResponse
{
    public string Id { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string Email { get; set; } = default!;
    public bool IsActive { get; set; }
    public bool IsAdmin { get; set; }
}
