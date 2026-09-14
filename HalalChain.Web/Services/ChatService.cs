namespace HalalChain.Services;

public interface IChatService
{
    Task<ChatResponse> SendMessageAsync(string message, string context = "", string persona = "general", CancellationToken ct = default);
    Task<List<string>> GetSuggestionsAsync(List<ChatMessage> history, CancellationToken ct = default);
}

public class ChatService : IChatService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ChatService> _logger;

    public ChatService(IHttpClientFactory httpClientFactory, ILogger<ChatService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<ChatResponse> SendMessageAsync(string message, string context = "", string persona = "general", CancellationToken ct = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("PlatformApi");
            var request = new { message, context, persona };
            var response = await client.PostAsJsonAsync("api/ai/chat", request, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ChatResponse>(ct) ?? new ChatResponse();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Chat API unreachable, using fallback");
            return GetFallbackResponse(message);
        }
    }

    public Task<List<string>> GetSuggestionsAsync(List<ChatMessage> history, CancellationToken ct = default)
    {
        var lastMessage = history.LastOrDefault(m => !m.IsUser)?.Content ?? "";
        var suggestions = new List<string>();

        if (lastMessage.Contains("product", StringComparison.OrdinalIgnoreCase))
        {
            suggestions.AddRange(new[] { "Show me similar products", "What's the halal status?", "Compare prices" });
        }
        else if (lastMessage.Contains("order", StringComparison.OrdinalIgnoreCase))
        {
            suggestions.AddRange(new[] { "Track my order", "Return policy", "Contact support" });
        }
        else
        {
            suggestions.AddRange(new[] { "Find halal products", "Track my order", "Vendor information", "Halal certification" });
        }

        return Task.FromResult(suggestions);
    }

    private static ChatResponse GetFallbackResponse(string message)
    {
        var lower = message.ToLowerInvariant();
        
        if (lower.Contains("halal") || lower.Contains("certif"))
        {
            return new ChatResponse
            {
                Reply = "HalalChain uses AI-powered verification through our Tawheed system. We analyze certificates, ingredients, and supplier data to ensure products meet halal standards. All verified products display a green 'Halal Verified' badge.",
                Sources = new[] { new SourceItem("Halal Standards", "/halal-standard") }
            };
        }
        
        if (lower.Contains("product") || lower.Contains("find") || lower.Contains("search"))
        {
            return new ChatResponse
            {
                Reply = "I can help you find halal products! You can browse our catalog using the search bar, filter by category, or look for the 'Halal Verified' badge. Would you like me to recommend some popular items?",
                Suggestions = new[] { "Show snacks", "Show beverages", "Halal verified only" }
            };
        }
        
        if (lower.Contains("order") || lower.Contains("track") || lower.Contains("shipping"))
        {
            return new ChatResponse
            {
                Reply = "To track your order, go to 'My Orders' in your account dashboard. You'll see real-time status updates. Most orders within Malaysia arrive within 3-5 business days.",
                Suggestions = new[] { "View my orders", "Shipping policy", "Return policy" }
            };
        }
        
        if (lower.Contains("vendor") || lower.Contains("sell") || lower.Contains("register"))
        {
            return new ChatResponse
            {
                Reply = "Interested in selling on HalalChain? Vendors can register through our Vendor Portal. We provide AI-powered tools for product listing, halal verification, and analytics to help you succeed.",
                Suggestions = new[] { "Register as vendor", "Vendor requirements", "Commission rates" }
            };
        }

        return new ChatResponse
        {
            Reply = "I'm your HalalChain AI assistant! I can help you with:\n\n• Finding halal-certified products\n• Order tracking and support\n• Halal certification questions\n• Vendor inquiries\n\nWhat would you like to know?",
            Suggestions = new[] { "Browse products", "Track order", "Halal info", "Vendor portal" }
        };
    }
}

public class ChatMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public bool IsUser { get; set; }
    public string Content { get; set; } = "";
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public List<SourceItem>? Sources { get; set; }
    public string[]? Reasoning { get; set; }
    public List<ToolCallInfo>? ToolCalls { get; set; }
    public bool IsLoading { get; set; }

    public ChatMessage() { }
    public ChatMessage(bool isUser, string content)
    {
        IsUser = isUser;
        Content = content;
    }
}

public class ChatResponse
{
    public string Reply { get; set; } = "";
    public SourceItem[]? Sources { get; set; }
    public string[]? Reasoning { get; set; }
    public string[]? Suggestions { get; set; }
    public List<ToolCallInfo>? ToolCalls { get; set; }
}

public class SourceItem
{
    public string Title { get; set; } = "";
    public string Url { get; set; } = "#";
    
    public SourceItem() { }
    public SourceItem(string title, string url) { Title = title; Url = url; }
}

public class ToolCallInfo
{
    public string ToolName { get; set; } = "";
    public string Status { get; set; } = "completed";
    public object? Parameters { get; set; }
    public string? Result { get; set; }
}
