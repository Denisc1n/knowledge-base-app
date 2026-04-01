using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.Validators;

namespace KnowledgeBase.Application.UnitTests;

public class SignupUserDtoValidatorTests
{
    private readonly SignupUserDtoValidator _validator = new();

    [Fact]
    public void Validate_WhenDtoIsValid_ShouldPass()
    {
        var dto = new SignupUserDto
        {
            FirstName = "Jane",
            LastName = "Doe",
            Username = "jane.doe",
            Email = "jane@example.com",
            Password = "Password123!"
        };

        var result = _validator.Validate(dto);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenUsernameContainsInvalidCharacters_ShouldFail()
    {
        var dto = new SignupUserDto
        {
            FirstName = "Jane",
            LastName = "Doe",
            Username = "jane doe",
            Email = "jane@example.com",
            Password = "Password123!"
        };

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SignupUserDto.Username));
    }

    [Fact]
    public void Validate_WhenPasswordIsTooShort_ShouldFail()
    {
        var dto = new SignupUserDto
        {
            FirstName = "Jane",
            LastName = "Doe",
            Username = "janedoe",
            Email = "jane@example.com",
            Password = "short"
        };

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SignupUserDto.Password));
    }
}

public class LoginDtoValidatorTests
{
    private readonly LoginDtoValidator _validator = new();

    [Fact]
    public void Validate_WhenDtoIsValid_ShouldPass()
    {
        var result = _validator.Validate(new LoginDto
        {
            Username = "janedoe",
            Password = "Password123!"
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenPasswordIsMissing_ShouldFail()
    {
        var result = _validator.Validate(new LoginDto
        {
            Username = "janedoe",
            Password = ""
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LoginDto.Password));
    }
}

public class RefreshTokenDtoValidatorTests
{
    private readonly RefreshTokenDtoValidator _validator = new();

    [Fact]
    public void Validate_WhenRefreshTokenIsPresent_ShouldPass()
    {
        var result = _validator.Validate(new RefreshTokenDto
        {
            RefreshToken = "some-refresh-token"
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenRefreshTokenIsMissing_ShouldFail()
    {
        var result = _validator.Validate(new RefreshTokenDto
        {
            RefreshToken = ""
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RefreshTokenDto.RefreshToken));
    }
}

public class ResetPasswordDtoValidatorTests
{
    private readonly ResetPasswordDtoValidator _validator = new();

    [Fact]
    public void Validate_WhenDtoIsValid_ShouldPass()
    {
        var result = _validator.Validate(new ResetPasswordDto
        {
            CurrentPassword = "CurrentPassword123!",
            NewPassword = "NewPassword123!"
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenNewPasswordMatchesCurrentPassword_ShouldFail()
    {
        var result = _validator.Validate(new ResetPasswordDto
        {
            CurrentPassword = "SamePassword123!",
            NewPassword = "SamePassword123!"
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ResetPasswordDto.NewPassword));
    }
}
