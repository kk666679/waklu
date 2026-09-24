using HalalChain.Application.Policies;
using HalalChain.Platform.Api.Persistence;
using HalalChain.Platform.Api.Modules.Events;
using HalalChain.Platform.Contracts.Api.Errors;
using HalalChain.Platform.Contracts.Vendors.Dto;
using HalalChain.Platform.Contracts.Vendors.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HalalChain.Platform.Api.Modules.Vendors;

[ApiController]
[Route("api/v1/vendors")]
[ApiVersion("1.0")]
public sealed class VendorsController(HalalChainDbContext db, IEventBus eventBus) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = CatalogPolicies.AdminOnly)]
    public async Task<ActionResult<VendorDto>> RegisterVendor([FromBody] RegisterVendorRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new ErrorResponse("INVALID_NAME", "Vendor name is required."));

        var vendor = new Vendor
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Slug = request.Name.ToLowerInvariant().Replace(" ", "-"),
            Country = request.Country,
        };

        db.Vendors.Add(vendor);
        await eventBus.PublishAsync(new VendorApprovedEvent(vendor.Id, vendor.Name), ct);
        await db.SaveChangesAsync(ct);

        return Ok(ToDto(vendor));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = CatalogPolicies.AdminOnly)]
    public async Task<ActionResult<VendorDto>> UpdateVendor(Guid id, [FromBody] UpdateVendorRequest request, CancellationToken ct)
    {
        var vendor = await db.Vendors.FindAsync([id], ct);
        if (vendor is null)
            return NotFound(new ErrorResponse("VENDOR_NOT_FOUND", $"Vendor {id} not found."));

        if (request.Name is not null) vendor.Name = request.Name;
        if (request.Country is not null) vendor.Country = request.Country;
        if (request.Status is not null) vendor.Status = request.Status;

        await db.SaveChangesAsync(ct);
        return Ok(ToDto(vendor));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = CatalogPolicies.AdminOnly)]
    public async Task<ActionResult> DeleteVendor(Guid id, CancellationToken ct)
    {
        var vendor = await db.Vendors.FindAsync([id], ct);
        if (vendor is null)
            return NotFound(new ErrorResponse("VENDOR_NOT_FOUND", $"Vendor {id} not found."));

        db.Vendors.Remove(vendor);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<VendorDto>> GetVendor(Guid id, CancellationToken ct)
    {
        var v = await db.Vendors.FindAsync([id], ct);
        return v is null
            ? NotFound(new ErrorResponse("VENDOR_NOT_FOUND", $"Vendor {id} not found."))
            : Ok(ToDto(v));
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<VendorDto[]>> ListVendors(CancellationToken ct)
    {
        var vendors = await db.Vendors.OrderBy(v => v.Name).ToListAsync(ct);
        return Ok(vendors.Select(ToDto).ToArray());
    }

    private static VendorDto ToDto(Vendor v) => new()
    {
        Id = v.Id,
        Name = v.Name,
        Slug = v.Slug,
        Status = v.Status,
        Country = v.Country,
        CreatedAt = v.CreatedAt,
    };
}
