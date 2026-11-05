using Tumiia.Domain.Documents;
using Tumiia.Domain.Projects;

namespace Tumiia.UnitTests.Domain;

public class ProjectAndDocumentTests
{
    private static readonly UploadFile Quote = new("Quote ABC.pdf", "application/pdf", 18_450, new string('A', 64));

    [Fact]
    public void CreateProject_WhenNameHasSpaces_TrimsIt()
    {
        var project = Project.Create("  Mokoena Residence ", Guid.NewGuid(), DateTimeOffset.UtcNow);

        project.Name.ShouldBe("Mokoena Residence");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateProject_WhenNameIsEmpty_IsRefused(string name)
    {
        var create = () => Project.Create(name, Guid.NewGuid(), DateTimeOffset.UtcNow);

        create.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void Owner_WhenProjectIsCreated_IsTheCreator()
    {
        var project = Project.Create("Site", Guid.NewGuid(), DateTimeOffset.UtcNow);

        var member = ProjectMember.Owner(project);

        member.UserId.ShouldBe(project.OwnerUserId);
        member.Role.ShouldBe(ProjectRole.Owner);
    }

    [Fact]
    public void AwaitUpload_WhenCreated_WaitsForItsFileUnderTheOriginalsKey()
    {
        var projectId = Guid.NewGuid();

        var document = Document.AwaitUpload(projectId, Quote, CaptureSource.DragDrop, Guid.NewGuid());

        document.Status.ShouldBe(DocumentStatus.PendingUpload);
        document.StorageKey.ShouldBe($"originals/{projectId}/{document.Id}");
        document.Sha256.ShouldBe(new string('a', 64));
        document.UploadTimestamp.ShouldBeNull();
        document.IsUploaded.ShouldBeFalse();
    }

    [Fact]
    public void MarkUploaded_WhenWaiting_MakesItNewAndRecordsTheTime()
    {
        var document = Document.AwaitUpload(Guid.NewGuid(), Quote, CaptureSource.FilePicker, Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;

        document.MarkUploaded(now);

        document.Status.ShouldBe(DocumentStatus.New);
        document.UploadTimestamp.ShouldBe(now);
        document.IsUploaded.ShouldBeTrue();
    }

    [Fact]
    public void MarkUploaded_WhenAlreadyUploaded_IsRefused()
    {
        var document = Document.AwaitUpload(Guid.NewGuid(), Quote, CaptureSource.FilePicker, Guid.NewGuid());
        document.MarkUploaded(DateTimeOffset.UtcNow);

        var markAgain = () => document.MarkUploaded(DateTimeOffset.UtcNow);

        markAgain.ShouldThrow<InvalidOperationException>();
    }
}
