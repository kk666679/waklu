using System.Collections.Concurrent;

namespace HalalChain.DataFlow.Models;

public sealed class DataFlowBatch<T>
{
    public DataFlowBatch()
    {
        Records = new List<T>();
        Metadata = new ConcurrentDictionary<string, object>();
    }

    public string BatchId { get; set; } = Guid.NewGuid().ToString();
    public string PipelineName { get; set; } = string.Empty;
    public long BatchNumber { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public List<T> Records { get; set; }
    public int RecordCount => Records?.Count ?? 0;
    public DataFlowBatchStatus Status { get; set; } = DataFlowBatchStatus.Pending;
    public string? ErrorMessage { get; set; }
    public Exception? Exception { get; set; }
    public ConcurrentDictionary<string, object> Metadata { get; set; }
    public long BytesProcessed { get; set; }
    public int RetryCount { get; set; }
}

public enum DataFlowBatchStatus
{
    Pending,
    Extracting,
    Extracted,
    Transforming,
    Transformed,
    Validating,
    Validated,
    Loading,
    Loaded,
    Completed,
    Failed,
    DeadLettered
}

public sealed class DataFlowRecord<T>
{
    public DataFlowRecord()
    {
        Fields = new Dictionary<string, object?>();
        Metadata = new Dictionary<string, object>();
    }

    public string RecordId { get; set; } = Guid.NewGuid().ToString();
    public T? Data { get; set; }
    public Dictionary<string, object?> Fields { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
    public DataFlowRecordStatus Status { get; set; } = DataFlowRecordStatus.Pending;
    public string? ErrorMessage { get; set; }
    public List<ValidationError> ValidationErrors { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; set; }
}

public enum DataFlowRecordStatus
{
    Pending,
    Extracted,
    Transformed,
    Validated,
    Loaded,
    Failed,
    Skipped,
    DeadLettered
}

public sealed class ValidationError
{
    public string FieldName { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public object? AttemptedValue { get; set; }
    public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;
}

public enum ValidationSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

public sealed class DataFlowMetrics
{
    public string PipelineName { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public long TotalRecordsExtracted { get; set; }
    public long TotalRecordsTransformed { get; set; }
    public long TotalRecordsValidated { get; set; }
    public long TotalRecordsLoaded { get; set; }
    public long TotalRecordsFailed { get; set; }
    public long TotalRecordsSkipped { get; set; }
    public long TotalBytesProcessed { get; set; }
    public int TotalBatches { get; set; }
    public int FailedBatches { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan ExtractDuration { get; set; }
    public TimeSpan TransformDuration { get; set; }
    public TimeSpan ValidateDuration { get; set; }
    public TimeSpan LoadDuration { get; set; }
    public Dictionary<string, long> EntityCounts { get; set; } = new();
    public Dictionary<string, int> ErrorCounts { get; set; } = new();
    public double ThroughputRecordsPerSecond => TotalDuration.TotalSeconds > 0
        ? TotalRecordsLoaded / TotalDuration.TotalSeconds
        : 0;
    public double SuccessRate => (TotalRecordsExtracted > 0)
        ? (double)TotalRecordsLoaded / TotalRecordsExtracted * 100
        : 100;
}

public sealed class DataFlowCheckpoint
{
    public string PipelineName { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;
    public string WatermarkColumn { get; set; } = string.Empty;
    public object WatermarkValue { get; set; } = new();
    public long LastProcessedBatchNumber { get; set; }
    public long LastProcessedRecordId { get; set; }
    public DateTimeOffset CheckpointAt { get; set; } = DateTimeOffset.UtcNow;
    public Dictionary<string, object> AdditionalState { get; set; } = new();
}

public sealed class DataFlowErrorRecord
{
    public string ErrorId { get; set; } = Guid.NewGuid().ToString();
    public string PipelineName { get; set; } = string.Empty;
    public string BatchId { get; set; } = string.Empty;
    public string? RecordId { get; set; }
    public DataFlowStage Stage { get; set; }
    public string ErrorType { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string? SourceData { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public int RetryCount { get; set; }
    public bool IsResolved { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public string? Resolution { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
}

public enum DataFlowStage
{
    Extract,
    Transform,
    Validate,
    Load,
    Checkpoint,
    Unknown
}