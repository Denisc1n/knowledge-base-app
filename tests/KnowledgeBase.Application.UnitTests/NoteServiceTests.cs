using KnowledgeBase.Application.DTOs;
using KnowledgeBase.Application.Services;
using KnowledgeBase.Domain.Abstractions;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Domain.Enums;
using NSubstitute;

namespace KnowledgeBase.Application.UnitTests;

public class NoteServiceTests
{
    private const string UserId = "user-123";

    private readonly INoteRepository _repository;
    private readonly NoteService _service;

    public NoteServiceTests()
    {
        _repository = Substitute.For<INoteRepository>();
        _service = new NoteService(_repository);
    }

    [Fact]
    public async Task CreateAsync_NormalizesInput_AssignsUserId_AndReturnsMappedDto()
    {
        var dto = new CreateNoteDto
        {
            Title = "  My title  ",
            Content = "  My content  ",
            Tags = [" DotNet ", "dotnet", "  CSharp  ", " "],
            Category = "  Backend  ",
            Status = NoteStatus.Published
        };

        _repository.CreateAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var note = callInfo.ArgAt<Note>(0);
                note.Id = "note-1";
                return note;
            });

        var result = await _service.CreateAsync(UserId, dto);

        await _repository.Received(1).CreateAsync(
            Arg.Is<Note>(n =>
                n.UserId == UserId &&
                n.Title == "My title" &&
                n.Content == "My content" &&
                n.Category == "Backend" &&
                n.Status == NoteStatus.Published &&
                n.Tags.SequenceEqual(new[] { "dotnet", "csharp" })),
            Arg.Any<CancellationToken>());

        Assert.Equal("note-1", result.Id);
        Assert.Equal("My title", result.Title);
        Assert.Equal("My content", result.Content);
        Assert.Equal("Backend", result.Category);
        Assert.Equal(NoteStatus.Published, result.Status);
        Assert.Equal(["dotnet", "csharp"], result.Tags);
    }

    [Fact]
    public async Task GetByIdAsync_WhenRepositoryReturnsNull_ReturnsNull()
    {
        _repository.GetByIdAsync("missing-id", UserId, Arg.Any<CancellationToken>())
            .Returns((Note?)null);

        var result = await _service.GetByIdAsync("missing-id", UserId);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_WhenNoteDoesNotExistForUser_ReturnsFalse_AndSkipsUpdate()
    {
        _repository.GetByIdAsync("missing-id", UserId, Arg.Any<CancellationToken>())
            .Returns((Note?)null);

        var result = await _service.UpdateAsync("missing-id", UserId, new UpdateNoteDto
        {
            Title = "Title",
            Content = "Content",
            Tags = ["Tag"],
            Category = "Category",
            Status = NoteStatus.Draft
        });

        Assert.False(result);
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Note>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenOwnedNoteExists_UpdatesNormalizedFields_AndReturnsRepositoryResult()
    {
        var existing = new Note
        {
            Id = "note-2",
            UserId = UserId,
            Title = "Old",
            Content = "Old",
            Tags = ["old"],
            Category = "Old",
            Status = NoteStatus.Draft,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
            UpdatedAtUtc = DateTime.UtcNow.AddDays(-1)
        };

        _repository.GetByIdAsync("note-2", UserId, Arg.Any<CancellationToken>())
            .Returns(existing);
        _repository.UpdateAsync(existing, Arg.Any<CancellationToken>())
            .Returns(true);

        var before = DateTime.UtcNow;

        var result = await _service.UpdateAsync("note-2", UserId, new UpdateNoteDto
        {
            Title = "  Updated title  ",
            Content = "  Updated content  ",
            Tags = [" DotNet ", "dotnet", " api ", ""],
            Category = "  Knowledge  ",
            Status = NoteStatus.Published
        });

        var after = DateTime.UtcNow;

        Assert.True(result);
        Assert.Equal("Updated title", existing.Title);
        Assert.Equal("Updated content", existing.Content);
        Assert.Equal("Knowledge", existing.Category);
        Assert.Equal(NoteStatus.Published, existing.Status);
        Assert.Equal(["dotnet", "api"], existing.Tags);
        Assert.InRange(existing.UpdatedAtUtc, before, after);
        await _repository.Received(1).UpdateAsync(existing, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PatchAsync_NormalizesValues_AndForwardsUserScopeToRepository()
    {
        var dto = new PatchNoteDto
        {
            Title = "  Patched title  ",
            Content = "  Patched content  ",
            Tags = [" CSharp ", "csharp", "  ", "Tests "],
            Category = "  Refactoring  ",
            Status = NoteStatus.Archived
        };

        _repository.PatchAsync(
                "note-3",
                UserId,
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<List<string>?>(),
                Arg.Any<string?>(),
                Arg.Any<NoteStatus?>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _service.PatchAsync("note-3", UserId, dto);

        Assert.True(result);
        await _repository.Received(1).PatchAsync(
            "note-3",
            UserId,
            "Patched title",
            "Patched content",
            Arg.Is<List<string>>(x => x.SequenceEqual(new[] { "csharp", "tests" })),
            "Refactoring",
            NoteStatus.Archived,
            Arg.Is<DateTime>(d => d > DateTime.UtcNow.AddMinutes(-1)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PatchAsync_WhenTagsAreNull_ForwardsNullTagsWithinUserScope()
    {
        _repository.PatchAsync(
                "note-4",
                UserId,
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<List<string>?>(),
                Arg.Any<string?>(),
                Arg.Any<NoteStatus?>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        await _service.PatchAsync("note-4", UserId, new PatchNoteDto { Title = "  Keep only title  " });

        await _repository.Received(1).PatchAsync(
            "note-4",
            UserId,
            "Keep only title",
            null,
            null,
            null,
            null,
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }
}
