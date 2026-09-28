namespace HalalChain.Application.Storage;

public readonly record struct WorkflowRunId(Guid Value)
{
    public static WorkflowRunId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("D");
}
