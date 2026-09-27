using HalalChain.Application.Storage;

namespace HalalChain.Storage.Integrity;

public sealed class HashMismatchException : Exception
{
    public BlobRef Expected { get; }
    public string Actual { get; }

    public HashMismatchException(BlobRef expected, string actual)
        : base($"Blob integrity failure: expected {expected.ContentHash}, got {actual}.")
    {
        Expected = expected;
        Actual = actual;
    }
}
