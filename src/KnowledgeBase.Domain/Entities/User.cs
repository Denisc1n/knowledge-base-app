using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace KnowledgeBase.Domain.Entities;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;

    public string FirstName { get; set; } = default!;

    public string LastName { get; set; } = default!;

    public string Username { get; set; } = default!;

    public string Email { get; set; } = default!;

    public string PasswordHash { get; set; } = default!;

    public string SecurityStamp { get; set; } = default!;

    public DateTime CreatedAtUtc { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsAdmin { get; set; }

    public static User Create(
        string firstName,
        string lastName,
        string username,
        string email,
        string passwordHash,
        DateTime createdAtUtc)
    {
        return new User
        {
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Username = NormalizeUsername(username),
            Email = NormalizeEmail(email),
            PasswordHash = passwordHash,
            SecurityStamp = CreateSecurityStamp(),
            CreatedAtUtc = createdAtUtc,
            IsActive = true,
            IsAdmin = false
        };
    }

    public void UpdatePassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        RotateSecurityStamp();
    }

    public void RotateSecurityStamp()
    {
        SecurityStamp = CreateSecurityStamp();
    }

    public static string NormalizeUsername(string username) =>
        username.Trim().ToLowerInvariant();

    public static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();

    private static string CreateSecurityStamp() =>
        Guid.NewGuid().ToString("N");
}
