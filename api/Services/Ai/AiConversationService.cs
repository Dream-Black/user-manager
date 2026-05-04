using Microsoft.EntityFrameworkCore;
using ProjectHub.Api.Data;
using ProjectHub.Api.Models;

namespace ProjectHub.Api.Services.Ai;

public class AiConversationService
{
    private readonly AppDbContext _context;

    public AiConversationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Conversation>> GetConversationsAsync()
    {
        return await _context.Conversations
            .Include(c => c.Messages)
            .OrderByDescending(c => c.IsPinned)
            .ThenByDescending(c => c.UpdatedAt)
            .ToListAsync();
    }

    public async Task<Conversation> CreateConversationAsync(string? title = null)
    {
        var conversation = new Conversation
        {
            Title = title ?? "新对话",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();
        return conversation;
    }

    public async Task<List<ChatMessage>> GetConversationMessagesAsync(int conversationId)
    {
        return await _context.ChatMessages
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task DeleteConversationAsync(int conversationId)
    {
        var conversation = await _context.Conversations.FindAsync(conversationId);
        if (conversation != null)
        {
            _context.Conversations.Remove(conversation);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<ChatMessage?> UpdateMessageAsync(int conversationId, int messageId, string content)
    {
        var message = await _context.ChatMessages
            .FirstOrDefaultAsync(m => m.Id == messageId && m.ConversationId == conversationId);

        if (message == null) return null;
        if (message.Role != "user")
            throw new InvalidOperationException("仅支持编辑用户消息");

        var originalCreatedAt = message.CreatedAt;
        message.Content = content;

        var downstreamMessages = await _context.ChatMessages
            .Where(m => m.ConversationId == conversationId && m.CreatedAt > originalCreatedAt)
            .ToListAsync();

        if (downstreamMessages.Count > 0)
            _context.ChatMessages.RemoveRange(downstreamMessages);

        var conversation = await _context.Conversations.FindAsync(conversationId);
        if (conversation != null)
        {
            conversation.UpdatedAt = DateTime.Now;
            if (conversation.Title == "新对话" && !string.IsNullOrWhiteSpace(content))
            {
                conversation.Title = content.Length > 30 ? content[..30] + "..." : content;
            }
        }

        await _context.SaveChangesAsync();
        return message;
    }

    public async Task<ChatMessage?> RegenerateFromMessageAsync(int conversationId, int messageId)
    {
        var target = await _context.ChatMessages
            .FirstOrDefaultAsync(m => m.Id == messageId && m.ConversationId == conversationId);
        if (target == null) return null;
        if (target.Role != "assistant")
            throw new InvalidOperationException("仅支持对助手回复进行重新生成");

        var messagesToRemove = await _context.ChatMessages
            .Where(m => m.ConversationId == conversationId && m.CreatedAt >= target.CreatedAt)
            .ToListAsync();

        _context.ChatMessages.RemoveRange(messagesToRemove);
        var conversation = await _context.Conversations.FindAsync(conversationId);
        if (conversation != null)
            conversation.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        return target;
    }
}
