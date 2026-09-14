// Global using aliases for the API project.
// CertificateStatus and ComplianceStatus are owned by HalalChain.Domain.Halal
// (the authoritative location). The DTO copies in
// HalalChain.Platform.Contracts.Halal.Dto exist for cross-project DTO use,
// but the entities — and therefore the EF Core queries that compare to them
// — use the Domain versions. Since both enums have identical numeric values,
// aliasing globally lets the entire API treat "CertificateStatus" and
// "ComplianceStatus" as the Domain types without per-file using directives.
global using CertificateStatus = HalalChain.Domain.Halal.CertificateStatus;
global using ComplianceStatus = HalalChain.Domain.Halal.ComplianceStatus;
global using HalalChain.Domain.Catalog;
global using HalalChain.Domain.Commerce;
global using HalalChain.Domain.Halal;
global using HalalChain.Domain.Vendors;
global using FluentValidation;
global using MediatR;
global using HalalChain.Application.Catalog.Handlers;
global using HalalChain.Application.Catalog.Validators;
global using HalalChain.Application.Common.Behaviors;
