using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Tumiia.Application.Auth;
using Tumiia.Application.Documents;
using Tumiia.Application.Projects;
using Tumiia.Domain.Documents;
using Tumiia.Domain.Projects;
using Tumiia.Domain.Storage;

namespace Tumiia.UnitTests.Application;

/// <summary>The fakes the project and document use-case tests need: a member, their project, and fake storage.</summary>
public sealed class DocumentTestDoubles
{
    public static readonly Guid MemberId = Guid.NewGuid();

    public static readonly Guid OutsiderId = Guid.NewGuid();

    public static readonly Project Project = global::Tumiia.Domain.Projects.Project.Create("Site", MemberId, DateTimeOffset.UtcNow);

    public IProjectStore Projects { get; } = Substitute.For<IProjectStore>();

    public IDocumentStore Documents { get; } = Substitute.For<IDocumentStore>();

    public IFileStore Files { get; } = Substitute.For<IFileStore>();

    public IDocumentQueue Queue { get; } = Substitute.For<IDocumentQueue>();

    public IAuditLog AuditLog { get; } = Substitute.For<IAuditLog>();

    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 11, 5, 9, 0, 0, TimeSpan.Zero));

    public UploadRules Rules { get; } = new() { AllowedExtensions = new HashSet<string> { ".pdf", ".dwg" } };

    public DocumentTestDoubles()
    {
        Projects.FindForMemberAsync(Project.Id, MemberId, Arg.Any<CancellationToken>()).Returns(new ProjectSummary(Project, ProjectRole.Owner));
        Files.CreateUploadUrlAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<long>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(call => $"https://s3.test/{call.ArgAt<string>(0)}");
        Documents.AddOrGetExistingAsync(Arg.Any<Document>(), Arg.Any<CancellationToken>()).Returns(call => call.ArgAt<Document>(0));
    }

    public ProjectAccessGuard Access() => new(Projects, Documents);

    /// <summary>A document in the member's project, stored in the fake store.</summary>
    public Document StoredDocument(string fileName = "quote.pdf", bool uploaded = false)
    {
        var document = Document.AwaitUpload(Project.Id, new UploadFile(fileName, "application/pdf", 1000, new string('c', 64)), CaptureSource.DragDrop, MemberId);
        if (uploaded)
        {
            document.MarkUploaded(Clock.GetUtcNow());
        }

        Documents.FindAsync(document.Id, Arg.Any<CancellationToken>()).Returns(document);
        return document;
    }
}
