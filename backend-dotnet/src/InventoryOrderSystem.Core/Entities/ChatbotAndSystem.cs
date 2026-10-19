namespace InventoryOrderSystem.Core.Entities;

public class ChatbotConversation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SessionId { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public string Channel { get; set; } = "WebChat";
    public string? Metadata { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

    public ICollection<ChatbotMessage> Messages { get; set; } = new List<ChatbotMessage>();
}

public class ChatbotMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    public ChatbotConversation Conversation { get; set; } = null!;
    public string Sender { get; set; } = "Customer"; // Customer, Bot, System
    public string MessageText { get; set; } = string.Empty;
    public string? IntentDetected { get; set; }
    public string? StructuredData { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ApplicationSetting
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SettingKey { get; set; } = string.Empty;
    public string SettingValue { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = "General";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
