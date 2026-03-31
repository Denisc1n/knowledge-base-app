using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.Validators;
using KnowledgeBase.Domain.Enums;

namespace KnowledgeBase.Application.UnitTests;

public class CreateNoteDtoValidatorTests
{
    private readonly CreateNoteDtoValidator _validator = new();

    [Fact]
    public void Validate_WhenDtoIsValid_ShouldPass()
    {
        var dto = new CreateNoteDto
        {
            Title = "Valid title",
            Content = "Valid content",
            Category = "General",
            Tags = ["dotnet", "testing"],
            Status = NoteStatus.Draft
        };

        var result = _validator.Validate(dto);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenTitleIsEmpty_ShouldFailForTitle()
    {
        var dto = new CreateNoteDto
        {
            Title = "",
            Content = "Valid content",
            Category = "General",
            Tags = ["dotnet"]
        };

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateNoteDto.Title));
    }

    [Fact]
    public void Validate_WhenTagExceeds40Chars_ShouldFailForTags()
    {
        var dto = new CreateNoteDto
        {
            Title = "Valid title",
            Content = "Valid content",
            Category = "General",
            Tags = [new string('x', 41)]
        };

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.StartsWith(nameof(CreateNoteDto.Tags)));
    }
}

public class UpdateNoteDtoValidatorTests
{
    private readonly UpdateNoteDtoValidator _validator = new();

    [Fact]
    public void Validate_WhenDtoIsValid_ShouldPass()
    {
        var dto = new UpdateNoteDto
        {
            Title = "Updated title",
            Content = "Updated content",
            Category = "Backend",
            Tags = ["api"],
            Status = NoteStatus.Published
        };

        var result = _validator.Validate(dto);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenContentExceedsMaxLength_ShouldFailForContent()
    {
        var dto = new UpdateNoteDto
        {
            Title = "Valid title",
            Content = new string('c', 10001),
            Category = "General",
            Tags = ["dotnet"],
            Status = NoteStatus.Draft
        };

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateNoteDto.Content));
    }
}

public class PatchNoteDtoValidatorTests
{
    private readonly PatchNoteDtoValidator _validator = new();

    [Fact]
    public void Validate_WhenNoFieldsProvided_ShouldFail()
    {
        var dto = new PatchNoteDto();

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("At least one field must be provided."));
    }

    [Fact]
    public void Validate_WhenAnyProvidedFieldIsInvalid_ShouldFailForThatField()
    {
        var dto = new PatchNoteDto
        {
            Title = "   "
        };

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PatchNoteDto.Title));
    }

    [Fact]
    public void Validate_WhenTagsContainWhitespaceOnlyItem_ShouldFailWithTagMessage()
    {
        var dto = new PatchNoteDto
        {
            Tags = ["valid", " "]
        };

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Tags cannot contain empty values."));
    }

    [Fact]
    public void Validate_WhenPatchDtoIsValid_ShouldPass()
    {
        var dto = new PatchNoteDto
        {
            Category = "Architecture",
            Status = NoteStatus.Archived
        };

        var result = _validator.Validate(dto);

        Assert.True(result.IsValid);
    }
}
