using Tumiia.Domain.Documents;

namespace Tumiia.UnitTests.Domain;

public class UploadRulesTests
{
    private static readonly string Hash = new('b', 64);

    private readonly UploadRules _rules = new()
    {
        AllowedExtensions = new HashSet<string> { ".pdf", ".dwg" },
        MaxFileSizeMb = 50,
        MaxFilesPerRequest = 50,
    };

    [Theory]
    [InlineData("plan.dwg")]
    [InlineData("QUOTE.PDF")]
    public void Check_WhenExtensionIsAllowed_AcceptsAnyCase(string fileName)
    {
        var problem = _rules.Check(new UploadFile(fileName, "application/octet-stream", 100, Hash));

        problem.ShouldBeNull();
    }

    [Fact]
    public void Check_WhenExtensionIsNotAllowed_SaysWhichTypesAre()
    {
        var problem = _rules.Check(new UploadFile("virus.exe", "application/octet-stream", 100, Hash));

        problem.ShouldBe("TUMIIA can't take .exe files. Allowed: .dwg .pdf.");
    }

    [Fact]
    public void Check_WhenThereIsNoExtension_IsRefused()
    {
        var problem = _rules.Check(new UploadFile("README", "text/plain", 100, Hash));

        problem.ShouldNotBeNull();
        problem.ShouldContain("without an extension");
    }

    [Theory]
    [InlineData(0, "The file is empty.")]
    [InlineData(50L * 1024 * 1024 + 1, "The file is larger than the 50 MB limit.")]
    public void Check_WhenSizeIsOutOfRange_IsRefused(long sizeBytes, string expected)
    {
        var problem = _rules.Check(new UploadFile("a.pdf", "application/pdf", sizeBytes, Hash));

        problem.ShouldBe(expected);
    }

    [Fact]
    public void Check_WhenExactlyTheLimit_IsAccepted()
    {
        var problem = _rules.Check(new UploadFile("a.pdf", "application/pdf", 50L * 1024 * 1024, Hash));

        problem.ShouldBeNull();
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz")]
    public void Check_WhenHashIsNotSha256_IsRefused(string sha256)
    {
        var problem = _rules.Check(new UploadFile("a.pdf", "application/pdf", 10, sha256));

        problem.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(0, "Send at least one file.")]
    [InlineData(51, "Send at most 50 files at a time.")]
    [InlineData(50, null)]
    public void CheckBatchSize_WhenCountIsOutOfRange_IsRefused(int count, string? expected)
    {
        var problem = _rules.CheckBatchSize(count);

        problem.ShouldBe(expected);
    }
}
