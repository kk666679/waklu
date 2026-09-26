namespace HalalChain.Marketplace.Services;

using HalalChain.Models;
using System.Text.Json;

/// <summary>
/// Dynamic pricing rules engine for promotions, tiered discounts, A/B testing, and competitive pricing.
/// Enables vendors to create complex pricing rules without coding.
/// </summary>
public interface IDynamicPricingRulesEngine
{
    /// <summary>
    /// Create a new pricing rule.
    /// </summary>
    Task<PricingRule> CreateRuleAsync(CreatePricingRuleRequest request, CancellationToken ct = default);

    /// <summary>
    /// Update an existing pricing rule.
    /// </summary>
    Task<PricingRule> UpdateRuleAsync(Guid ruleId, UpdatePricingRuleRequest request, CancellationToken ct = default);

    /// <summary>
    /// Delete a pricing rule.
    /// </summary>
    Task DeleteRuleAsync(Guid ruleId, CancellationToken ct = default);

    /// <summary>
    /// Get all rules for a vendor.
    /// </summary>
    Task<List<PricingRule>> GetVendorRulesAsync(int vendorId, CancellationToken ct = default);

    /// <summary>
    /// Get all rules for a specific product.
    /// </summary>
    Task<List<PricingRule>> GetProductRulesAsync(int productId, CancellationToken ct = default);

    /// <summary>
    /// Evaluate applicable pricing rules for a product/customer combination.
    /// </summary>
    Task<PricingEvaluation> EvaluatePriceAsync(int productId, int quantity, string? customerSegment = null, CancellationToken ct = default);

    /// <summary>
    /// Apply a specific rule to calculate price.
    /// </summary>
    Task<decimal> ApplyRuleAsync(PricingRule rule, decimal basePrice, int quantity, CancellationToken ct = default);

    /// <summary>
    /// Get rules applicable within a date range.
    /// </summary>
    Task<List<PricingRule>> GetActiveRulesAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default);

    /// <summary>
    /// Simulate pricing rule impact on revenue.
    /// </summary>
    Task<PricingSimulation> SimulateRuleAsync(int productId, PricingRule rule, CancellationToken ct = default);
}

/// <summary>
/// Implementation of dynamic pricing rules engine.
/// </summary>
public class DynamicPricingRulesEngine : IDynamicPricingRulesEngine
{
    private readonly ILogger<DynamicPricingRulesEngine> _logger;
    
    // In-memory store (use database in production)
    private readonly Dictionary<Guid, PricingRule> _rules = [];
    private readonly Dictionary<int, List<Guid>> _productRules = []; // productId -> ruleIds

    public DynamicPricingRulesEngine(ILogger<DynamicPricingRulesEngine> logger)
    {
        _logger = logger;
    }

    public async Task<PricingRule> CreateRuleAsync(CreatePricingRuleRequest request, CancellationToken ct = default)
    {
        try
        {
            // Validate rule
            ValidateRule(request);

            var rule = new PricingRule
            {
                Id = Guid.NewGuid(),
                VendorId = request.VendorId,
                ProductIds = request.ProductIds,
                Name = request.Name,
                Description = request.Description,
                RuleType = request.RuleType,
                Conditions = request.Conditions,
                PriceAdjustment = request.PriceAdjustment,
                AdjustmentType = request.AdjustmentType,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                IsActive = request.IsActive,
                Priority = request.Priority ?? 100,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _rules[rule.Id] = rule;

            // Index by product
            foreach (var productId in request.ProductIds)
            {
                if (!_productRules.ContainsKey(productId))
                    _productRules[productId] = [];
                _productRules[productId].Add(rule.Id);
            }

            _logger.LogInformation("Pricing rule created: RuleId={RuleId}, Name={Name}, VendorId={VendorId}",
                rule.Id, rule.Name, request.VendorId);

            return rule;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating pricing rule for VendorId: {VendorId}", request.VendorId);
            throw;
        }
    }

    public async Task<PricingRule> UpdateRuleAsync(Guid ruleId, UpdatePricingRuleRequest request, CancellationToken ct = default)
    {
        try
        {
            if (!_rules.TryGetValue(ruleId, out var rule))
                throw new KeyNotFoundException($"Rule {ruleId} not found.");

            // Update fields
            if (!string.IsNullOrEmpty(request.Name)) rule.Name = request.Name;
            if (!string.IsNullOrEmpty(request.Description)) rule.Description = request.Description;
            if (request.PriceAdjustment.HasValue) rule.PriceAdjustment = request.PriceAdjustment.Value;
            if (!string.IsNullOrEmpty(request.AdjustmentType)) rule.AdjustmentType = request.AdjustmentType;
            if (request.IsActive.HasValue) rule.IsActive = request.IsActive.Value;
            if (request.Conditions != null) rule.Conditions = request.Conditions;
            if (request.StartDate.HasValue) rule.StartDate = request.StartDate.Value;
            if (request.EndDate.HasValue) rule.EndDate = request.EndDate.Value;

            rule.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Pricing rule updated: RuleId={RuleId}, Name={Name}", ruleId, rule.Name);

            return rule;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating pricing rule: RuleId={RuleId}", ruleId);
            throw;
        }
    }

    public async Task DeleteRuleAsync(Guid ruleId, CancellationToken ct = default)
    {
        try
        {
            if (!_rules.TryGetValue(ruleId, out var rule))
                throw new KeyNotFoundException($"Rule {ruleId} not found.");

            // Remove from product indices
            foreach (var productId in rule.ProductIds)
            {
                if (_productRules.TryGetValue(productId, out var ruleIds))
                    ruleIds.Remove(ruleId);
            }

            _rules.Remove(ruleId);

            _logger.LogInformation("Pricing rule deleted: RuleId={RuleId}", ruleId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting pricing rule: RuleId={RuleId}", ruleId);
            throw;
        }
    }

    public async Task<List<PricingRule>> GetVendorRulesAsync(int vendorId, CancellationToken ct = default)
    {
        try
        {
            return _rules.Values
                .Where(r => r.VendorId == vendorId)
                .OrderByDescending(r => r.Priority)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vendor rules for VendorId: {VendorId}", vendorId);
            throw;
        }
    }

    public async Task<List<PricingRule>> GetProductRulesAsync(int productId, CancellationToken ct = default)
    {
        try
        {
            if (!_productRules.TryGetValue(productId, out var ruleIds))
                return new List<PricingRule>();

            return ruleIds
                .Select(id => _rules[id])
                .Where(r => r.IsActive)
                .OrderByDescending(r => r.Priority)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product rules for ProductId: {ProductId}", productId);
            throw;
        }
    }

    public async Task<PricingEvaluation> EvaluatePriceAsync(int productId, int quantity, string? customerSegment = null, CancellationToken ct = default)
    {
        try
        {
            var evaluation = new PricingEvaluation
            {
                ProductId = productId,
                Quantity = quantity,
                CustomerSegment = customerSegment ?? "standard",
                BasePrice = 0, // Would fetch from product
                EvaluatedAt = DateTime.UtcNow
            };

            // Get applicable rules
            var applicableRules = await GetProductRulesAsync(productId, default);

            // Filter by date and customer segment
            applicableRules = applicableRules
                .Where(r =>
                {
                    var now = DateTime.UtcNow;
                    var dateMatch = r.StartDate <= now && (r.EndDate == null || r.EndDate >= now);
                    var segmentMatch = r.CustomerSegments == null || r.CustomerSegments.Contains(customerSegment);
                    return dateMatch && segmentMatch;
                })
                .ToList();

            // Evaluate each rule
            foreach (var rule in applicableRules)
            {
                if (EvaluateConditions(rule.Conditions, quantity))
                {
                    var adjustedPrice = await ApplyRuleAsync(rule, evaluation.BasePrice, quantity, default);
                    evaluation.AppliedRules.Add(new AppliedPricingRule
                    {
                        RuleId = rule.Id,
                        RuleName = rule.Name,
                        AdjustedPrice = adjustedPrice
                    });

                    // Only apply highest priority rule (override)
                    break;
                }
            }

            evaluation.FinalPrice = evaluation.AppliedRules.Any()
                ? evaluation.AppliedRules.First().AdjustedPrice
                : evaluation.BasePrice;

            return evaluation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating price for ProductId: {ProductId}", productId);
            throw;
        }
    }

    public async Task<decimal> ApplyRuleAsync(PricingRule rule, decimal basePrice, int quantity, CancellationToken ct = default)
    {
        try
        {
            return rule.AdjustmentType?.ToLowerInvariant() switch
            {
                "percentage" => basePrice * (1 - (rule.PriceAdjustment / 100)),
                "fixed" => Math.Max(0, basePrice - rule.PriceAdjustment),
                "multiplier" => basePrice * rule.PriceAdjustment,
                _ => basePrice
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying pricing rule: RuleId={RuleId}", rule.Id);
            throw;
        }
    }

    public async Task<List<PricingRule>> GetActiveRulesAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken ct = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            startDate ??= now.AddDays(-1);
            endDate ??= now.AddDays(30);

            return _rules.Values
                .Where(r => r.IsActive && r.StartDate <= endDate && (r.EndDate == null || r.EndDate >= startDate))
                .OrderByDescending(r => r.Priority)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active rules");
            throw;
        }
    }

    public async Task<PricingSimulation> SimulateRuleAsync(int productId, PricingRule rule, CancellationToken ct = default)
    {
        try
        {
            var simulation = new PricingSimulation
            {
                ProductId = productId,
                RuleId = rule.Id,
                SimulatedAt = DateTime.UtcNow
            };

            // Simulate on different quantities
            var quantities = new[] { 1, 5, 10, 25, 50, 100 };
            var basePrice = 100m; // Placeholder

            foreach (var qty in quantities)
            {
                var adjustedPrice = await ApplyRuleAsync(rule, basePrice, qty, ct);
                var discount = ((basePrice - adjustedPrice) / basePrice) * 100;

                simulation.ScenarioResults.Add(new PricingScenario
                {
                    Quantity = qty,
                    BasePrice = basePrice,
                    AdjustedPrice = adjustedPrice,
                    DiscountPercent = discount,
                    EstimatedRevenue = adjustedPrice * qty
                });
            }

            _logger.LogInformation("Pricing rule simulated: RuleId={RuleId}, ProductId={ProductId}",
                rule.Id, productId);

            return simulation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error simulating pricing rule: RuleId={RuleId}", rule.Id);
            throw;
        }
    }

    // Private helpers

    private void ValidateRule(CreatePricingRuleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Rule name is required.");

        if (!request.ProductIds.Any())
            throw new ArgumentException("At least one product is required.");

        if (request.PriceAdjustment < 0)
            throw new ArgumentException("Price adjustment cannot be negative.");

        if (request.StartDate > request.EndDate)
            throw new ArgumentException("Start date must be before end date.");
    }

    private bool EvaluateConditions(string? conditionsJson, int quantity)
    {
        if (string.IsNullOrEmpty(conditionsJson))
            return true; // No conditions = always apply

        try
        {
            var conditions = JsonSerializer.Deserialize<PricingConditions>(conditionsJson);
            if (conditions == null) return true;

            // Check minimum quantity
            if (conditions.MinQuantity.HasValue && quantity < conditions.MinQuantity)
                return false;

            // Check maximum quantity
            if (conditions.MaxQuantity.HasValue && quantity > conditions.MaxQuantity)
                return false;

            return true;
        }
        catch
        {
            return true; // Silently fail to default condition
        }
    }
}

// DTOs

public class PricingRule
{
    public Guid Id { get; set; }
    public int VendorId { get; set; }
    public List<int> ProductIds { get; set; } = [];
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string RuleType { get; set; } = "Promotion"; // Promotion, TieredDiscount, Flash, ABTest, Competitive
    public string? Conditions { get; set; } // JSON: {minQuantity, maxQuantity, dayOfWeek, etc.}
    public decimal PriceAdjustment { get; set; }
    public string AdjustmentType { get; set; } = "Percentage"; // Percentage, Fixed, Multiplier
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; } = 100;
    public List<string>? CustomerSegments { get; set; } // wholesale, vip, new, etc.
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreatePricingRuleRequest
{
    public int VendorId { get; set; }
    public List<int> ProductIds { get; set; } = [];
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string RuleType { get; set; } = "Promotion";
    public string? Conditions { get; set; }
    public decimal PriceAdjustment { get; set; }
    public string AdjustmentType { get; set; } = "Percentage";
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public int? Priority { get; set; }
}

public class UpdatePricingRuleRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? PriceAdjustment { get; set; }
    public string? AdjustmentType { get; set; }
    public bool? IsActive { get; set; }
    public string? Conditions { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class PricingEvaluation
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public string CustomerSegment { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public decimal FinalPrice { get; set; }
    public List<AppliedPricingRule> AppliedRules { get; set; } = [];
    public DateTime EvaluatedAt { get; set; }
}

public class AppliedPricingRule
{
    public Guid RuleId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public decimal AdjustedPrice { get; set; }
}

public class PricingSimulation
{
    public int ProductId { get; set; }
    public Guid RuleId { get; set; }
    public DateTime SimulatedAt { get; set; }
    public List<PricingScenario> ScenarioResults { get; set; } = [];
}

public class PricingScenario
{
    public int Quantity { get; set; }
    public decimal BasePrice { get; set; }
    public decimal AdjustedPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal EstimatedRevenue { get; set; }
}

public class PricingConditions
{
    public int? MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }
    public List<string>? DaysOfWeek { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}
