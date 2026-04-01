namespace KnowledgeBase.Application.DTOs;

public class ResetPasswordDto
{
    public string CurrentPassword { get; set; } = default!;
    public string NewPassword { get; set; } = default!;
}
