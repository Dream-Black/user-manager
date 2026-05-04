namespace ProjectHub.Api.Dtos;

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public bool DeepThink { get; set; } = false;
    public string? Attachments { get; set; }
    public string? Model { get; set; }
}

public class ContinueRequest
{
    public string ToolResult { get; set; } = string.Empty;
}

public class CreateConversationRequest
{
    public string? Title { get; set; }
}

public class UpdateAiSettingsRequest
{
    public string? DeepSeekApiKey { get; set; }
    public string? DeepSeekModel { get; set; }
}

public class ArchiveConversationRequest
{
    public bool IsArchived { get; set; }
}

public class PinConversationRequest
{
    public bool IsPinned { get; set; }
}

public class UpdateMessageRequest
{
    public string Content { get; set; } = string.Empty;
}