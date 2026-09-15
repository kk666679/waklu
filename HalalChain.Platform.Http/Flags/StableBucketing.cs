namespace HalalChain.Platform.Http.Flags;

public static class StableBucketing
{
    public static int Compute(Guid userId, string flagName, int modulus)
    {
        var data = $"{userId:N}:{flagName}";
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(data));
        var value = BitConverter.ToUInt32(hash, 0);
        return (int)(value % (uint)modulus);
    }
}