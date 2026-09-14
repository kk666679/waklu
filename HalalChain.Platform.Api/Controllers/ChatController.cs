using HalalChain.Platform.Contracts.AI.Requests;
using HalalChain.Platform.Contracts.AI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalalChain.Platform.Api.Controllers;

[ApiController]
[Route("api/ai")]
public sealed class ChatController : ControllerBase
{
    [HttpPost("chat")]
    [AllowAnonymous]
    public ActionResult<ChatResponse> Chat([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { error = "Message is required." });

        var message = request.Message.Trim();
        var lower = message.ToLowerInvariant();

        if (lower.Contains("halal") || lower.Contains("certif"))
        {
            return Ok(new ChatResponse(
                "HalalChain uses AI-powered verification through our Tawheed system. We analyze certificates, ingredients, and supplier data to ensure products meet halal standards. All verified products display a 'Halal Verified' badge.",
                Sources: new SourceItem[] { new("Halal Standards", "/halal-standard") }
            ));
        }

        if (lower.Contains("product") || lower.Contains("find") || lower.Contains("search"))
        {
            return Ok(new ChatResponse(
                "I can help you find halal products! You can browse our catalog using the search bar, filter by category, or look for the 'Halal Verified' badge. Would you like me to recommend some popular items?",
                Suggestions: new string[] { "Show snacks", "Show beverages", "Halal verified only" }
            ));
        }

        if (lower.Contains("order") || lower.Contains("track") || lower.Contains("shipping"))
        {
            return Ok(new ChatResponse(
                "To track your order, go to 'My Orders' in your account dashboard. You'll see real-time status updates. Most orders within Malaysia arrive within 3-5 business days.",
                Suggestions: new string[] { "View my orders", "Shipping policy", "Return policy" }
            ));
        }

        if (lower.Contains("vendor") || lower.Contains("sell") || lower.Contains("register"))
        {
            return Ok(new ChatResponse(
                "Interested in selling on HalalChain? Vendors can register through our Vendor Portal. We provide AI-powered tools for product listing, halal verification, and analytics to help you succeed.",
                Suggestions: new string[] { "Register as vendor", "Vendor requirements", "Commission rates" }
            ));
        }

        return Ok(new ChatResponse(
            "I'm your HalalChain AI assistant! I can help you with:\n\n• Finding halal-certified products\n• Order tracking and support\n• Halal certification questions\n• Vendor inquiries\n\nWhat would you like to know?",
            Suggestions: new string[] { "Browse products", "Track order", "Halal info", "Vendor portal" }
        ));
    }
}
