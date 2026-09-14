using HalalChain.Platform.Api.Persistence;
using HalalChain.Platform.Contracts.Commerce.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HalalChain.Platform.Api.Controllers;

[ApiController]
[Route("api/commerce")]
public sealed class CustomersController(HalalChainDbContext db) : ControllerBase
{
    [HttpGet("customers")]
    [Authorize(Roles = "admin,verification-officer")]
    public async Task<ActionResult<CustomerDto[]>> GetCustomers(CancellationToken ct)
    {
        var customers = await db.Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new CustomerDto(
                Guid.Parse(g.Key),
                g.Key,
                g.Key + "@example.com",
                g.Count(),
                g.Sum(o => o.Total),
                g.First().Currency,
                g.Min(o => o.CreatedAt)))
            .OrderByDescending(c => c.TotalSpent)
            .ToArrayAsync(ct);

        return Ok(customers);
    }
}
