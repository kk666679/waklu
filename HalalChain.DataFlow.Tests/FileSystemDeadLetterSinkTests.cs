using HalalChain.DataFlow.Models;
using HalalChain.DataFlow.Validation;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HalalChain.DataFlow.Tests;

public sealed class FileSystemDeadLetterSinkTests : IDisposable
{
    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), "halalchain-tests", Guid.NewGuid().ToString("N"));

    private readonly FileSystemDeadLetterSink _sink;

    public FileSystemDeadLetterSinkTests() =>
        _sink = new FileSystemDeadLetterSink(_directory, NullLogger<FileSystemDeadLetterSink>.Instance);

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
        GC.SuppressFinalize(this);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static DataFlowErrorRecord Error(
        string pipeline = "cert-sync",
        string errorType = "ValidationFailed") => new()
    {
        PipelineName = pipeline,
        Stage = DataFlowStage.Validate,
        ErrorType = errorType,
        ErrorMessage = "Certificate number is missing.",
        SourceData = "{\"CertificateNumber\":null}"
    };

    [Fact]
    public async Task WriteAsync_CreatesDirectoryAndPersistsRecord()
    {
        await _sink.WriteAsync(Error(), Token);

        var files = Directory.GetFiles(_directory, "deadletter-*.ndjson");
        Assert.Single(files);
    }

    [Fact]
    public async Task WrittenRecord_RoundTrips()
    {
        await _sink.WriteAsync(Error(), Token);

        var records = await _sink.ReadAsync(cancellationToken: Token);

        var record = Assert.Single(records);
        Assert.Equal("cert-sync", record.PipelineName);
        Assert.Equal("ValidationFailed", record.ErrorType);
        Assert.Equal("{\"CertificateNumber\":null}", record.SourceData);
    }

    [Fact]
    public async Task WriteBatchAsync_PersistsEveryRecord()
    {
        await _sink.WriteBatchAsync([Error(), Error("shipment-sync"), Error("lot-sync")], Token);

        var records = await _sink.ReadAsync(cancellationToken: Token);

        Assert.Equal(3, records.Count);
    }

    [Fact]
    public async Task WriteBatchAsync_WithNoRecords_WritesNothing()
    {
        await _sink.WriteBatchAsync([], Token);

        Assert.Empty(Directory.GetFiles(_directory));
    }

    [Fact]
    public async Task ReadAsync_RespectsMaxRecords()
    {
        await _sink.WriteBatchAsync([Error(), Error(), Error(), Error()], Token);

        var records = await _sink.ReadAsync(maxRecords: 2, cancellationToken: Token);

        Assert.Equal(2, records.Count);
    }

    [Fact]
    public async Task ReadAsync_SkipsBlankLines()
    {
        await _sink.WriteAsync(Error(), Token);
        var file = Directory.GetFiles(_directory, "deadletter-*.ndjson")[0];
        await File.AppendAllTextAsync(file, Environment.NewLine + "   " + Environment.NewLine, Token);

        var records = await _sink.ReadAsync(cancellationToken: Token);

        Assert.Single(records);
    }

    [Fact]
    public async Task ReadAsync_SkipsTruncatedTrailingLine()
    {
        await _sink.WriteAsync(Error(), Token);
        var file = Directory.GetFiles(_directory, "deadletter-*.ndjson")[0];

        // Simulate a crash mid-write: valid JSON followed by a partial line.
        await File.AppendAllTextAsync(file, "{\"PipelineName\":\"trunc", Token);

        var records = await _sink.ReadAsync(cancellationToken: Token);

        Assert.Single(records);
    }

    [Fact]
    public async Task PurgeAsync_DeletesFilesOlderThanCutoff()
    {
        var old = DateTimeOffset.UtcNow.AddDays(-10);
        var oldFile = Path.Combine(_directory, $"deadletter-{old:yyyy-MM-dd}.ndjson");
        await File.WriteAllTextAsync(oldFile, "{}" + Environment.NewLine, Token);

        var removed = await _sink.PurgeAsync(DateTimeOffset.UtcNow.AddDays(-1), Token);

        Assert.Equal(1, removed);
        Assert.False(File.Exists(oldFile));
    }

    [Fact]
    public async Task PurgeAsync_KeepsFilesNewerThanCutoff()
    {
        await _sink.WriteAsync(Error(), Token);

        // "Older than yesterday" must not touch a file written today.
        var removed = await _sink.PurgeAsync(DateTimeOffset.UtcNow.AddDays(-1), Token);

        Assert.Equal(0, removed);
        Assert.NotEmpty(Directory.GetFiles(_directory));
    }

    [Fact]
    public async Task PurgeAsync_LeavesUnparseableFilenamesAlone()
    {
        // Retention must never delete a file it cannot date.
        var stray = Path.Combine(_directory, "deadletter-not-a-date.ndjson");
        await File.WriteAllTextAsync(stray, "{}", Token);

        var removed = await _sink.PurgeAsync(DateTimeOffset.UtcNow.AddDays(-1), Token);

        Assert.Equal(0, removed);
        Assert.True(File.Exists(stray));
    }
}
