namespace HalalChain.Application.Storage;

public interface IAccessLog
{
    Task RecordAsync(AccessRecord record, CancellationToken ct = default);
}
