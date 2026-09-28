using System.ComponentModel.DataAnnotations;

namespace HalalChain.DataFlow.Configuration;

public enum DatabaseProvider
{
    PostgreSQL,
    SqlServer
}

public enum SyncMode
{
    Full,
    Incremental,
    ChangeDataCapture
}

public enum ConflictResolution
{
    SourceWins,
    DestinationWins,
    Merge,
    Fail
}

public sealed class DataFlowConnectionOptions
{
    public const string SectionName = "DataFlow:Connections";

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DatabaseProvider Provider { get; set; }

    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    public int CommandTimeoutSeconds { get; set; } = 300;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 5;
    public bool EnableSensitiveDataLogging { get; set; } = false;
    public Dictionary<string, string> AdditionalProperties { get; set; } = new();
}

public sealed class DataFlowSourceOptions
{
    public const string SectionName = "DataFlow:Sources";

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string ConnectionName { get; set; } = string.Empty;

    [Required]
    public string EntityName { get; set; } = string.Empty;

    public string? SchemaName { get; set; }
    public string? TableName { get; set; }
    public string? Query { get; set; }
    public List<string> Columns { get; set; } = new();
    public string? WatermarkColumn { get; set; }
    public string? WatermarkValue { get; set; }
    public int BatchSize { get; set; } = 10000;
    public int MaxDegreeOfParallelism { get; set; } = 4;
    public Dictionary<string, string> Parameters { get; set; } = new();
}

public sealed class DataFlowDestinationOptions
{
    public const string SectionName = "DataFlow:Destinations";

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string ConnectionName { get; set; } = string.Empty;

    [Required]
    public string EntityName { get; set; } = string.Empty;

    public string? SchemaName { get; set; }
    public string? TableName { get; set; }
    public List<string> Columns { get; set; } = new();
    public SyncMode SyncMode { get; set; } = SyncMode.Incremental;
    public ConflictResolution ConflictResolution { get; set; } = ConflictResolution.SourceWins;
    public List<string> KeyColumns { get; set; } = new();
    public List<string> UpdateColumns { get; set; } = new();
    public List<string> IgnoreColumns { get; set; } = new();
    public int BatchSize { get; set; } = 5000;
    public bool UseBulkInsert { get; set; } = true;
    public bool DisableConstraints { get; set; } = false;
    public bool RecreateIndexes { get; set; } = false;
    public Dictionary<string, string> AdditionalProperties { get; set; } = new();
}

public sealed class DataFlowPipelineOptions
{
    public const string SectionName = "DataFlow:Pipelines";

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string SourceName { get; set; } = string.Empty;

    [Required]
    public string DestinationName { get; set; } = string.Empty;

    public string? TransformationName { get; set; }
    public string? ValidationName { get; set; }
    public bool EnableValidation { get; set; } = true;
    public bool EnableTransformation { get; set; } = true;
    public int MaxDegreeOfParallelism { get; set; } = 2;
    public int BatchSize { get; set; } = 5000;
    public TimeSpan? Timeout { get; set; }
    public Dictionary<string, string> Parameters { get; set; } = new();
}

public sealed class DataFlowOptions
{
    public const string SectionName = "DataFlow";

    public List<DataFlowConnectionOptions> Connections { get; set; } = new();
    public List<DataFlowSourceOptions> Sources { get; set; } = new();
    public List<DataFlowDestinationOptions> Destinations { get; set; } = new();
    public List<DataFlowPipelineOptions> Pipelines { get; set; } = new();
    public GlobalDataFlowOptions Global { get; set; } = new();
}

public sealed class GlobalDataFlowOptions
{
    public bool EnableMetrics { get; set; } = true;
    public bool EnableDetailedLogging { get; set; } = false;
    public int DefaultBatchSize { get; set; } = 5000;
    public int DefaultCommandTimeoutSeconds { get; set; } = 300;
    public int DefaultMaxRetryAttempts { get; set; } = 3;
    public int DefaultRetryDelaySeconds { get; set; } = 5;
    public string? DeadLetterDirectory { get; set; }
    public bool EnableDeadLetterQueue { get; set; } = true;
}