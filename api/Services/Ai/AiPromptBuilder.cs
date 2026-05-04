using System.Text.Json;
using ProjectHub.Api.Models;

namespace ProjectHub.Api.Services.Ai;

public class AiPromptBuilder
{
    public List<object> BuildMessageList(List<ChatMessage> history, bool deepThink, Conversation conversation)
    {
        var messages = new List<object>();

        var memorySummary = string.IsNullOrWhiteSpace(conversation.MemorySummary)
            ? ""
            : $"\n\n用户偏好摘要：{conversation.MemorySummary}";

        var draftFormat = BuildDraftSchemaPrompt();
        var permissionNote = "你具备生成新增/编辑草案的能力。你不能直接落库，但可以输出待确认草案。不要说'我没有权限'，应表述为'需要你确认后由系统执行'。";
        var terminalNote = "你还具备在桌面端环境执行终端命令的能力。当需要执行系统命令时，先调用终端使用说明工具获取正确用法，再使用 <terminal>命令内容</terminal> 格式包裹命令。命令会被自动执行，执行结果会返回给你继续处理。";
        var terminalRestriction = "重要限制：你没有 execute_command 或任何终端执行工具，禁止直接声称自己可以执行终端命令。如果需要执行命令（dir, git, node 等），必须先理解终端使用说明，然后在回复中用 <terminal>命令</terminal> 标签包裹命令，前端会自动执行。";
        var terminalGuideNote = "当用户要求你查看项目架构、目录结构、文件清单、代码分布，或者需要你先了解当前仓库再分析时，必须先调用终端使用说明工具获取正确的命令格式与建议。调用该工具后，只能调用一次，不要重复调用；接下来必须基于返回结果直接给出一个可执行的终端命令，并用 <terminal>命令内容</terminal> 包裹它。";
        var terminalGuideRestriction = "如果已经调用过终端使用说明工具，后续不要再次调用它；不要只复述建议或只做推理，必须输出具体的终端命令。对于架构/目录类需求，优先给出 cd 到项目根目录后的 dir / tree / rg 命令。";
        var systemPrompt = deepThink
            ? $"{permissionNote} {terminalNote} {terminalRestriction} {terminalGuideNote} {terminalGuideRestriction} 你是 ProjectHub 的个人工作助理，具备数据库查询能力。你可以调用工具来获取项目、任务等数据。请用中文回答，给出具体的分析和建议。当你需要数据时，直接调用相应的工具函数获取实时数据。若你判断用户意图是新增/编辑项目、任务、资源，请先生成一个结构化写入草案，但不要直接写库；草案必须是严格 JSON，并且只放在回复末尾，格式必须符合：{draftFormat}。正文要先解释依据。{memorySummary}"
            : $"{permissionNote} {terminalNote} {terminalRestriction} {terminalGuideNote} {terminalGuideRestriction} 你是 ProjectHub 的个人工作助理，可以用中文帮助用户查询项目进度、任务状态等。请简洁专业地回答。当用户询问今天该做什么或项目情况时，请先调用工具查询实时数据再回答。若你判断用户意图是新增/编辑项目、任务、资源，请先生成一个结构化写入草案，但不要直接写库；草案必须是严格 JSON，并且只放在回复末尾，格式必须符合：{draftFormat}。正文要先解释依据。{memorySummary}";

        messages.Add(new { role = "system", content = systemPrompt });

        foreach (var msg in history)
        {
            if (msg.Role == "system") continue;

            if (msg.Role == "assistant" && !string.IsNullOrEmpty(msg.ToolCalls))
            {
                messages.Add(new { role = "assistant", content = msg.Content ?? (string?)null });
                continue;
            }

            if (msg.Role == "tool") continue;

            messages.Add(new { role = msg.Role, content = msg.Content ?? "" });
        }

        return messages;
    }

    public static string BuildDraftSchemaPrompt()
    {
        return "json schema examples: " +
               "project.create => {\"id\":\"...\",\"schemaVersion\":1,\"kind\":\"project\",\"mode\":\"create\",\"title\":\"新增项目：官网重构\",\"targetLabel\":\"项目：官网重构\",\"before\":\"无\",\"after\":\"创建一个新的官网重构项目，默认状态为 active\",\"preview\":\"项目名称：官网重构；类型：work；状态：active\",\"status\":\"pending\",\"createdAt\":\"2026-04-25T00:00:00Z\",\"expiresAt\":\"2026-04-26T00:00:00Z\",\"payload\":{\"name\":\"官网重构\",\"type\":\"work\",\"status\":\"active\"}}; " +
               "project.update => {\"id\":\"...\",\"schemaVersion\":1,\"kind\":\"project\",\"mode\":\"update\",\"title\":\"更新项目：官网重构\",\"targetLabel\":\"项目ID:12\",\"before\":\"名称=旧名称\",\"after\":\"名称=新名称\",\"preview\":\"把项目 12 的名称改为 新名称\",\"status\":\"pending\",\"createdAt\":\"2026-04-25T00:00:00Z\",\"expiresAt\":\"2026-04-26T00:00:00Z\",\"payload\":{\"id\":12,\"name\":\"新名称\"}}; " +
               "task.create => {\"id\":\"...\",\"schemaVersion\":1,\"kind\":\"task\",\"mode\":\"create\",\"title\":\"新增任务：接口联调\",\"targetLabel\":\"项目ID:3\",\"before\":\"无\",\"after\":\"在项目 3 下创建任务 接口联调\",\"preview\":\"任务标题：接口联调；项目ID：3；优先级：medium；状态：todo\",\"status\":\"pending\",\"createdAt\":\"2026-04-25T00:00:00Z\",\"expiresAt\":\"2026-04-26T00:00:00Z\",\"payload\":{\"title\":\"接口联调\",\"projectId\":3,\"priority\":\"medium\",\"status\":\"todo\"}}; " +
               "task.update => {\"id\":\"...\",\"schemaVersion\":1,\"kind\":\"task\",\"mode\":\"update\",\"title\":\"更新任务：接口联调\",\"targetLabel\":\"任务ID:88\",\"before\":\"状态=todo\",\"after\":\"状态=in_progress\",\"preview\":\"把任务 88 状态改为进行中\",\"status\":\"pending\",\"createdAt\":\"2026-04-25T00:00:00Z\",\"expiresAt\":\"2026-04-26T00:00:00Z\",\"payload\":{\"id\":88,\"status\":\"in_progress\"}}; " +
               "resource.create => {\"id\":\"...\",\"schemaVersion\":1,\"kind\":\"resource\",\"mode\":\"create\",\"title\":\"新增资源：漫画库\",\"targetLabel\":\"电脑ID:1\",\"before\":\"无\",\"after\":\"在电脑 1 新增一个资源路径 /data/comics\",\"preview\":\"电脑ID：1；类型：comic；路径：/data/comics；启用：true\",\"status\":\"pending\",\"createdAt\":\"2026-04-25T00:00:00Z\",\"expiresAt\":\"2026-04-26T00:00:00Z\",\"payload\":{\"computerId\":1,\"type\":\"comic\",\"path\":\"/data/comics\",\"isEnabled\":true}}; " +
               "resource.update => {\"id\":\"...\",\"schemaVersion\":1,\"kind\":\"resource\",\"mode\":\"update\",\"title\":\"更新资源：漫画库\",\"targetLabel\":\"资源ID:7\",\"before\":\"path=/old\",\"after\":\"path=/new\",\"preview\":\"把资源 7 的路径改为 /new\",\"status\":\"pending\",\"createdAt\":\"2026-04-25T00:00:00Z\",\"expiresAt\":\"2026-04-26T00:00:00Z\",\"payload\":{\"id\":7,\"path\":\"/new\"}}";
    }
}
