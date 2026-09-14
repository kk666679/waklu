namespace HalalChain.Models;

public class ReportData
{
    public string Title { get; set; } = string.Empty;
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public List<ReportPoint> Points { get; set; } = new();
    public decimal Total { get; set; }
    public Dictionary<string, decimal> Breakdowns { get; set; } = new();
}

public class ReportPoint
{
    public DateTime Date { get; set; }
    public decimal Value { get; set; }
    public int Count { get; set; }
}
