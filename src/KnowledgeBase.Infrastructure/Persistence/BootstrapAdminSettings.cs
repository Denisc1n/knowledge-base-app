namespace KnowledgeBase.Infrastructure.Persistence;

public class BootstrapAdminSettings
{
    public const string SectionName = "BootstrapAdmin";

    public bool Enabled { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
}
