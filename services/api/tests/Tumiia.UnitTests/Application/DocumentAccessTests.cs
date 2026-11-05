using NSubstitute;
using Tumiia.Application.Documents;
using Tumiia.Application.Projects;
using Tumiia.Domain.Projects;

namespace Tumiia.UnitTests.Application;

public class DocumentAccessTests
{
    private readonly DocumentTestDoubles _fakes = new();

    private GetDocumentSourceUrl CreateSourceUrl()
    {
        _fakes.Files.CreateDownloadUrlAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns("https://s3.test/get");
        return new GetDocumentSourceUrl(_fakes.Access(), _fakes.Files, _fakes.Clock);
    }

    [Fact]
    public async Task FindAccessibleDocument_WhenCallerIsNotAMember_IsTreatedAsMissing()
    {
        var document = _fakes.StoredDocument();

        var found = await _fakes.Access().FindAccessibleDocumentAsync(DocumentTestDoubles.OutsiderId, document.Id, CancellationToken.None);

        found.ShouldBeNull();
    }

    [Fact]
    public async Task FindAccessibleDocument_WhenCallerIsAMember_ReturnsIt()
    {
        var document = _fakes.StoredDocument();

        var found = await _fakes.Access().FindAccessibleDocumentAsync(DocumentTestDoubles.MemberId, document.Id, CancellationToken.None);

        found.ShouldBe(document);
    }

    [Theory]
    [InlineData("quote.pdf", true)]
    [InlineData("site.JPG", true)]
    [InlineData("plan.dwg", false)]
    [InlineData("prices.xlsx", false)]
    public async Task GetSourceUrl_WhenUploaded_ShowsPdfsAndImagesAndDownloadsTheRest(string fileName, bool inline)
    {
        var document = _fakes.StoredDocument(fileName, uploaded: true);

        var result = await CreateSourceUrl().ExecuteAsync(DocumentTestDoubles.MemberId, document.Id, CancellationToken.None);

        result.Url.ShouldBe("https://s3.test/get");
        result.ExpiresAt.ShouldBe(_fakes.Clock.GetUtcNow().AddMinutes(5));
        await _fakes.Files.Received().CreateDownloadUrlAsync(document.StorageKey, fileName, inline, TimeSpan.FromMinutes(5), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSourceUrl_WhenNotUploadedYet_IsFoundButHasNoAddress()
    {
        var document = _fakes.StoredDocument(uploaded: false);

        var result = await CreateSourceUrl().ExecuteAsync(DocumentTestDoubles.MemberId, document.Id, CancellationToken.None);

        result.Found.ShouldBeTrue();
        result.Url.ShouldBeNull();
    }

    [Fact]
    public async Task CreateProject_WhenCalled_SavesTheProjectWithTheCallerAsOwner()
    {
        var summary = await new CreateProject(_fakes.Projects, _fakes.Clock).ExecuteAsync(DocumentTestDoubles.MemberId, "Mokoena Residence", CancellationToken.None);

        summary.Role.ShouldBe(ProjectRole.Owner);
        await _fakes.Projects.Received().AddAsync(summary.Project, Arg.Is<ProjectMember>(member => member.UserId == DocumentTestDoubles.MemberId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListDocuments_WhenCallerIsNotAMember_ReturnsNull()
    {
        var documents = await new ListDocuments(_fakes.Access(), _fakes.Documents).ExecuteAsync(DocumentTestDoubles.OutsiderId, DocumentTestDoubles.Project.Id, null, null, CancellationToken.None);

        documents.ShouldBeNull();
    }
}
