namespace HalalChain.Models;

public class HalalCertification
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string CertifyingBody { get; set; } = string.Empty;
    public string CertificateNumber { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? DocumentUrl { get; set; }
    public bool IsVerified { get; set; }
    public string? BlockchainTxHash { get; set; }
}
