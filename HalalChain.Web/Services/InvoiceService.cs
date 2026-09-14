using HalalChain.Models;

namespace HalalChain.Services;

public class InvoiceService : IInvoiceService
{
    private readonly List<Invoice> _invoices = new();
    private int _nextId = 1;

    public InvoiceService()
    {
        _invoices.Add(new Invoice { Id = _nextId++, InvoiceNumber = "INV-1001", OrderId = 1, Total = 58m, Status = InvoiceStatus.Paid });
        _invoices.Add(new Invoice { Id = _nextId++, InvoiceNumber = "INV-1002", OrderId = 2, Total = 63m, Status = InvoiceStatus.Issued });
    }

    public Task<IReadOnlyList<Invoice>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Invoice>>(_invoices.ToList());

    public Task<Invoice?> GetByIdAsync(int id, CancellationToken ct = default)
        => Task.FromResult(_invoices.FirstOrDefault(i => i.Id == id));

    public Task<Invoice> CreateAsync(Invoice invoice, CancellationToken ct = default)
    {
        invoice.Id = _nextId++;
        invoice.InvoiceNumber = $"INV-{1000 + invoice.Id}";
        _invoices.Add(invoice);
        return Task.FromResult(invoice);
    }

    public Task<Invoice> MarkPaidAsync(int id, CancellationToken ct = default)
    {
        var inv = _invoices.FirstOrDefault(i => i.Id == id);
        if (inv is not null) { inv.Status = InvoiceStatus.Paid; inv.PaidAt = DateTime.UtcNow; }
        return Task.FromResult(inv ?? new Invoice { Id = id });
    }

    public Task DeleteAsync(int id, CancellationToken ct = default)
    {
        _invoices.RemoveAll(i => i.Id == id);
        return Task.CompletedTask;
    }
}
