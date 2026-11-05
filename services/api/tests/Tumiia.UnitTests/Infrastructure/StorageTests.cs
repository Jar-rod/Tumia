using Tumiia.Domain.Documents;
using Tumiia.Infrastructure.Persistence.Configurations;
using Tumiia.Infrastructure.Storage;

namespace Tumiia.UnitTests.Infrastructure;

public class StorageTests
{
    [Fact]
    public void ContentDisposition_WhenNameIsPlain_UsesItAsIs()
    {
        var header = S3FileStore.ContentDisposition("Quote ABC.pdf", inline: true);

        header.ShouldStartWith("inline");
        header.ShouldContain("filename=\"Quote ABC.pdf\"");
    }

    [Fact]
    public void ContentDisposition_WhenNameHasNonEnglishLetters_AlsoSendsAnEncodedName()
    {
        var header = S3FileStore.ContentDisposition("Kwotasie – Ndlovu.dwg", inline: false);

        header.ShouldStartWith("attachment");
        header.ShouldContain("filename*=UTF-8''");
    }

    [Theory]
    [InlineData(CaptureSource.DragDrop, "drag_drop")]
    [InlineData(CaptureSource.FilePicker, "file_picker")]
    [InlineData(CaptureSource.MobileUpload, "mobile_upload")]
    public void CaptureSource_WhenStored_UsesTheV11Names(CaptureSource source, string text)
    {
        var stored = DocumentConfiguration.ToText(source);

        stored.ShouldBe(text);
        DocumentConfiguration.FromText(stored).ShouldBe(source);
    }
}
