# Halal Verification Feature

This document describes the Halal Verification feature in HalalChain.Marketplace, including the listing and detail views with full compliance tracking and evidence collection.

## Feature Overview

The Halal Verification feature provides:
- **Verification Listing** (`/verify`) - Browse all product verification results
- **Verification Detail** (`/verify/{id}`) - Deep dive into compliance metrics and evidence
- **Status Tracking** - Real-time compliance status (Verified, Under Review, Incomplete, etc.)
- **Evidence Audit Trail** - Complete history of collected evidence and verification decisions
- **Compliance Metrics** - Visual representation of evidence completeness and compliance scores

## Pages

### 1. Verification Listing (`/verify`)

**Route**: `/verify`  
**Component**: `Pages/Verify/IndexViewModel.razor`  
**ViewModel**: `Models/ViewModels/HalalVerificationViewModel.cs`  

#### Features:
- List all product verifications with pagination
- Filter by compliance status
- Display key verification metrics
  - Verification status (Verified, Under Review, etc.)
  - Policy version and jurisdiction
  - Missing evidence items
  - Human review alerts
- Quick links to product details and full verification details
- Responsive card-based layout

#### Data Flow:
```
IHalalVerificationRepository.GetByComplianceStatusAsync()
  ↓
HalalVerificationViewModel[] (mapped via AutoMapper)
  ↓
IndexViewModel.razor (renders cards with pagination)
```

#### Example Display:
```
┌─────────────────────────────────────────┐
│ Product Name                      ✓ Verified
│ Verified Jan 15, 2025
├─────────────────────────────────────────┤
│ Policy Version: MY-v3                   │
│ Jurisdiction: MY                        │
├─────────────────────────────────────────┤
│ [View product →]  [View details →]      │
└─────────────────────────────────────────┘
```

### 2. Verification Detail (`/verify/{id}`)

**Route**: `/verify/{id:guid}`  
**Component**: `Pages/Verify/DetailViewModel.razor`  
**ViewModels**: 
- `HalalVerificationViewModel` - Main verification data
- `VerificationEvidenceViewModel` - Evidence items collected
- `VerificationAuditViewModel` - Audit trail of decisions

#### Features:

**Header Section**:
- Product name with verification status badge
- Breadcrumb navigation
- Quick metrics (Status, Policy, Jurisdiction, Date)

**Evidence Collection Panel**:
- List all evidence items with:
  - Evidence type (Certificate, Documentation, Audit, Lab, Supplier, etc.)
  - Description and source
  - Collection status (Collected, Pending, Failed, Verified)
  - Timestamp of collection
  - Optional document links

**Compliance Details Panel**:
- Reason codes (why product was approved/rejected)
- Missing evidence items (what still needs to be collected)
- **Compliance Metrics**:
  - Evidence Completeness (0-100%)
  - Compliance Score (0-100%)
  - Visual progress bars with color coding

**Audit Trail Section**:
- Chronological log of verification events:
  - Created, Updated, Reviewed, Approved, Rejected
  - Performed by (officer name)
  - Notes and change history
  - Timestamp for each action

#### Data Flow:
```
IHalalVerificationRepository.GetByIdAsync(id)
  ↓ (includes Evidences & Audits)
IHalalVerificationRepository.GetEvidenceByVerificationAsync(id)
  ↓
IHalalVerificationRepository.GetAuditTrailAsync(id)
  ↓
ViewModels (mapped via AutoMapper)
  ↓
DetailViewModel.razor (renders full page)
```

## ViewModels

### HalalVerificationViewModel
```csharp
public class HalalVerificationViewModel
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }
    public string ComplianceStatus { get; set; } // Verified, ManualReview, etc.
    public string PolicyVersion { get; set; } // MY-v3, etc.
    public string Jurisdiction { get; set; } // MY, SG, ID, etc.
    public bool RequiresHumanReview { get; set; }
    public string[] ReasonCodes { get; set; }
    public string[] MissingEvidence { get; set; }
    public DateTime VerifiedAt { get; set; }
    public int EvidenceCompleteness { get; set; } // 0-100%
    public int ComplianceScore { get; set; } // 0-100%
    
    // Computed properties
    public string DisplayStatus { get; }
    public string StatusBadgeClass { get; }
}
```

### VerificationEvidenceViewModel
```csharp
public class VerificationEvidenceViewModel
{
    public Guid Id { get; set; }
    public Guid VerificationId { get; set; }
    public string EvidenceType { get; set; } // Certificate, Documentation, etc.
    public string Description { get; set; }
    public string Status { get; set; } // Collected, Pending, Failed, Verified
    public string? Source { get; set; } // Where evidence came from
    public string? DocumentUrl { get; set; } // Link to evidence document
    public DateTime CollectedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
}
```

### VerificationAuditViewModel
```csharp
public class VerificationAuditViewModel
{
    public Guid Id { get; set; }
    public Guid VerificationId { get; set; }
    public string ActionName { get; set; } // Created, Updated, Reviewed, etc.
    public string? PerformedBy { get; set; }
    public string? Notes { get; set; }
    public DateTime Timestamp { get; set; }
    public string? OldValue { get; set; } // For tracking changes
    public string? NewValue { get; set; }
}
```

## Repository Interface

### IHalalVerificationRepository

Key methods added for verification detail:
```csharp
/// Get evidence items for a verification
Task<IEnumerable<VerificationEvidence>> GetEvidenceByVerificationAsync(
    Guid verificationId, CancellationToken ct = default);

/// Get audit trail events for a verification
Task<IEnumerable<VerificationAudit>> GetAuditTrailAsync(
    Guid verificationId, CancellationToken ct = default);
```

Existing methods used:
```csharp
Task<HalalVerification?> GetByIdAsync(Guid id);
Task<IEnumerable<HalalVerification>> GetByComplianceStatusAsync(string status, skip, take);
Task<int> GetCountAsync();
```

## Status Values

### Compliance Status
- **Verified** ✓ - Product passed all compliance checks
- **ManualReview** ⟳ - Under review by compliance officer
- **Incomplete** ⊗ - Missing evidence, cannot verify yet
- **Hold** ⏸ - Verification paused pending additional info
- **NonCompliant** ✗ - Product failed compliance checks
- **Unverified** ? - Not yet verified

### Evidence Status
- **Collected** - Evidence successfully gathered and verified
- **Pending** - Evidence requested, awaiting submission
- **Failed** - Evidence collection failed or incomplete
- **Verified** - Evidence verified by compliance officer

## AutoMapper Mappings

Add these mappings to `Models/Mapping/MappingProfile.cs`:

```csharp
// Verification mapping
CreateMap<Domain.Halal.HalalVerification, HalalVerificationViewModel>()
    .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.Name))
    .ForMember(dest => dest.EvidenceCompleteness, opt => opt.Ignore())
    .ForMember(dest => dest.ComplianceScore, opt => opt.Ignore());

// Evidence mapping
CreateMap<Domain.Halal.VerificationEvidence, VerificationEvidenceViewModel>();

// Audit trail mapping
CreateMap<Domain.Halal.VerificationAudit, VerificationAuditViewModel>();
```

## Testing

### Test Cases

1. **List Verifications**
   - Navigate to `/verify`
   - Verify verifications load with correct status badges
   - Test pagination (if > 10 items)
   - Verify both "View product" and "View details" links work

2. **View Verification Detail**
   - Click "View details" from listing
   - Verify breadcrumbs appear
   - Check all metric cards display
   - Verify evidence items load with proper status badges
   - Check audit trail displays in chronological order

3. **Compliance Metrics**
   - Verify progress bars appear for Evidence Completeness
   - Verify progress bars appear for Compliance Score
   - Check color coding (green > 90%, yellow 50-90%, red < 50%)

4. **Evidence Display**
   - Verify evidence type badges display
   - Check status badges color-coded correctly
   - Verify timestamps are formatted consistently

5. **Audit Trail**
   - Verify events appear in reverse chronological order (newest first)
   - Check "Performed by" shows officer name
   - Verify notes display when present
   - Check timestamps are accurate

## Performance Considerations

### Query Optimization
- Use eager loading (.Include()) for Evidences and Audits
- Consider pagination for large audit trails (future enhancement)
- Add indexes on VerificationId in Evidence and Audit tables

### Caching
- Cache frequently accessed verifications by jurisdiction
- Cache recent verifications for dashboard
- Invalidate on update

### Example:
```csharp
// Efficient query with eager loading
var verification = await _context.HalalVerifications
    .Include(v => v.Evidences)
    .Include(v => v.Audits)
    .Include(v => v.Product)
    .FirstOrDefaultAsync(v => v.Id == id);
```

## Future Enhancements

1. **Advanced Filtering**
   - Filter by evidence type
   - Filter by date range
   - Search by policy version

2. **Evidence Management**
   - Upload evidence documents
   - View evidence documents/images
   - Evidence verification workflow

3. **Compliance Reports**
   - Export verification results as PDF
   - Generate compliance reports by jurisdiction
   - Batch verification operations

4. **Real-time Updates**
   - SignalR notifications when verification completes
   - Live updates to compliance scores
   - Alert on human review requirement

5. **Analytics**
   - Verification success rates
   - Average time to verification
   - Evidence collection patterns
   - Jurisdiction-specific metrics

## Related Features

- **Product Detail** (`/products/{id}`) - Shows inline verification status
- **Vendor Dashboard** - Includes verification metrics for vendor's products
- **Certificates** - Separate certificate tracking (complementary to verification)

## Links

- [Testing Guide](./TESTING_GUIDE.md) - How to test all components
- [Component Migration Guide](./COMPONENT_MIGRATION_GUIDE.md) - Repository pattern migration
- [HalalViewModel.cs](./Models/ViewModels/HalalViewModel.cs) - ViewModel definitions
