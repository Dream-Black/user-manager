using System.Text.Json.Nodes;

namespace ProjectHub.Api.Models;

public sealed class ActionDraftDto
{
    public string? Id { get; set; }
    public int SchemaVersion { get; set; } = 1;
    public string? Kind { get; set; }
    public string? Mode { get; set; }
    public string? Title { get; set; }
    public string? TargetLabel { get; set; }
    public string? Before { get; set; }
    public string? After { get; set; }
    public string? Preview { get; set; }
    public JsonObject? Payload { get; set; }
    public string? Status { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}