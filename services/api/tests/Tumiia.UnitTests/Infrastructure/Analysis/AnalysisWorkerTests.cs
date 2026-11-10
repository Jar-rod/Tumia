using System.Text;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Tumiia.Application.Analysis;
using Tumiia.Application.Auth;
using Tumiia.Application.Documents;
using Tumiia.Domain.Analysis;
using Tumiia.Domain.Documents;
using Tumiia.Domain.Storage;
using Tumiia.Infrastructure.Analysis;
using Tumiia.Infrastructure.Storage;
using Tumiia.UnitTests.Application;
using Tumiia.UnitTests.Analysis;

namespace Tumiia.UnitTests.Infrastructure.Analysis;

public class AnalysisWorkerTests
{
    private const string QueueUrl = "https://sqs.test/queue";

    private readonly IAmazonSQS _sqs = Substitute.For<IAmazonSQS>();
    private readonly AnalysisTestDoubles _fakes = new();
    private readonly IOptions<StorageSettings> _storage = Options.Create(new StorageSettings { DocumentAnalysisQueueUrl = QueueUrl });
    private readonly IOptions<AnalysisSettings> _settings = Options.Create(new AnalysisSettings());

    private AnalysisMessageHandler Handler()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_fakes.Documents.Documents);
        services.AddSingleton(_fakes.Documents.AuditLog);
        services.AddSingleton(_ => _fakes.AnalyseDocument());
        services.AddSingleton<FailDocumentAnalysis>();
        return new AnalysisMessageHandler(_sqs, services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(), _storage, _settings, NullLogger<AnalysisMessageHandler>.Instance);
    }

    private static Message MessageFor(Guid documentId, int receiveCount = 1) => new()
    {
        MessageId = "m1",
        ReceiptHandle = "r1",
        Body = $"{{\"documentId\":\"{documentId}\"}}",
        Attributes = new Dictionary<string, string> { [AnalysisMessageHandler.ReceiveCountAttribute] = receiveCount.ToString() },
    };

    [Fact]
    public async Task HandleAsync_WhenTheAnalysisSucceeds_DeletesTheMessage()
    {
        var document = _fakes.UploadedText(SampleDocuments.Quote);

        await Handler().HandleAsync(MessageFor(document.Id), CancellationToken.None);

        document.Status.ShouldBe(DocumentStatus.NeedsReview);
        await _sqs.Received().DeleteMessageAsync(QueueUrl, "r1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenTheAnalysisFailsBeforeTheLastTry_LeavesTheMessageForAnotherTry()
    {
        var document = _fakes.UploadedText(SampleDocuments.Quote);
        _fakes.Documents.Files.DownloadAsync(document.StorageKey, Arg.Any<CancellationToken>()).ThrowsAsync(new IOException("S3 hiccup"));

        await Handler().HandleAsync(MessageFor(document.Id, receiveCount: 1), CancellationToken.None);

        document.Status.ShouldBe(DocumentStatus.Processing);
        await _sqs.DidNotReceive().DeleteMessageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenTheAnalysisFailsOnTheLastTry_MarksTheDocumentFailed()
    {
        var document = _fakes.UploadedText(SampleDocuments.Quote);
        _fakes.Documents.Files.DownloadAsync(document.StorageKey, Arg.Any<CancellationToken>()).ThrowsAsync(new IOException("S3 down"));

        await Handler().HandleAsync(MessageFor(document.Id, receiveCount: 3), CancellationToken.None);

        document.Status.ShouldBe(DocumentStatus.Failed);
        document.FailureReason!.ShouldContain("after 3 tries");
        await _sqs.DidNotReceive().DeleteMessageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenTheMessageNamesNoDocument_DeletesIt()
    {
        await Handler().HandleAsync(new Message { MessageId = "m2", ReceiptHandle = "r2", Body = "not json" }, CancellationToken.None);

        await _sqs.Received().DeleteMessageAsync(QueueUrl, "r2", Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("{\"documentId\":\"00000000-0000-0000-0000-000000000000\"}")]
    [InlineData("null")]
    [InlineData(null)]
    public void ReadDocumentId_WhenTheBodyHasNoUsableId_ReturnsNull(string? body)
    {
        AnalysisMessageHandler.ReadDocumentId(body).ShouldBeNull();
    }

    [Fact]
    public void ReceiveCount_WhenSqsDidNotSendIt_CountsAsTheFirstTry()
    {
        AnalysisMessageHandler.ReceiveCount(new Message()).ShouldBe(1);
        AnalysisMessageHandler.ReceiveCount(MessageFor(Guid.NewGuid(), 2)).ShouldBe(2);
    }

    [Fact]
    public async Task ReceiveOnceAsync_AsksForTheReceiveCountAndHandlesEachMessage()
    {
        var document = _fakes.UploadedText(SampleDocuments.Quote);
        _sqs.ReceiveMessageAsync(Arg.Any<ReceiveMessageRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ReceiveMessageResponse { Messages = [MessageFor(document.Id)] });
        var worker = new DocumentAnalysisWorker(_sqs, Handler(), _storage, _settings, NullLogger<DocumentAnalysisWorker>.Instance);

        await worker.ReceiveOnceAsync(QueueUrl, CancellationToken.None);

        await _sqs.Received().ReceiveMessageAsync(
            Arg.Is<ReceiveMessageRequest>(request => request.WaitTimeSeconds == 20 && request.MaxNumberOfMessages == 1 && request.MessageSystemAttributeNames!.Contains(AnalysisMessageHandler.ReceiveCountAttribute)),
            Arg.Any<CancellationToken>());
        document.Status.ShouldBe(DocumentStatus.NeedsReview);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTheWorkerIsSwitchedOff_NeverReadsTheQueue()
    {
        var settings = Options.Create(new AnalysisSettings { WorkerEnabled = false });
        var worker = new DocumentAnalysisWorker(_sqs, Handler(), _storage, settings, NullLogger<DocumentAnalysisWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);

        await _sqs.DidNotReceive().ReceiveMessageAsync(Arg.Any<ReceiveMessageRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenReadingTheQueueFails_PausesInsteadOfCrashing()
    {
        var calls = 0;
        _sqs.ReceiveMessageAsync(Arg.Any<ReceiveMessageRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                calls++;
                return calls == 1 ? throw new AmazonSQSException("throttled") : new ReceiveMessageResponse();
            });
        var worker = new DocumentAnalysisWorker(_sqs, Handler(), _storage, _settings, NullLogger<DocumentAnalysisWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(200, TestContext.Current.CancellationToken);
        await worker.StopAsync(CancellationToken.None);

        calls.ShouldBe(1);
    }
}
