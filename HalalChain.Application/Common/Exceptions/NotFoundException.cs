namespace HalalChain.Application.Common.Exceptions;

public sealed class NotFoundException : Exception
{
    public NotFoundException(string name, object key)
        : base($"Resource \"{name}\" ({key}) was not found.") { }
}
