using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectHub.Api.Data;
using ProjectHub.Api.Models;

namespace ProjectHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotesController : ControllerBase
{
    private readonly AppDbContext _context;

    public NotesController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>获取所有笔记（支持按标签筛选、排序）</summary>
    [HttpGet]
    public async Task<IActionResult> GetAllNotes([FromQuery] string? tags = null, [FromQuery] string? sort = null)
    {
        var query = _context.Notes
            .Include(n => n.Tags)
            .AsQueryable();

        if (!string.IsNullOrEmpty(tags))
        {
            var tagList = tags.Split(',').ToList();
            query = query.Where(n => n.Tags.Any(t => tagList.Contains(t.TagId)));
        }

        sort = sort?.ToLower() ?? "updatedat";
        query = sort switch
        {
            "title" => query.OrderBy(n => n.Title),
            "tagcount" => query.OrderByDescending(n => n.Tags.Count),
            _ => query.OrderByDescending(n => n.UpdatedAt)
        };

        var notes = await query.ToListAsync();

        var result = notes.Select(n => new
        {
            id = n.Id,
            title = n.Title,
            content = n.Content,
            tagIds = n.Tags.Select(t => t.TagId).ToList(),
            createdAt = n.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
            updatedAt = n.UpdatedAt.ToString("yyyy-MM-dd HH:mm")
        });

        return Ok(result);
    }

    /// <summary>获取单个笔记</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetNote(int id)
    {
        var note = await _context.Notes
            .Include(n => n.Tags)
            .FirstOrDefaultAsync(n => n.Id == id);

        if (note == null)
            return NotFound(new { message = "笔记不存在" });

        var result = new
        {
            id = note.Id,
            title = note.Title,
            content = note.Content,
            tagIds = note.Tags.Select(t => t.TagId).ToList(),
            createdAt = note.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
            updatedAt = note.UpdatedAt.ToString("yyyy-MM-dd HH:mm")
        };

        return Ok(result);
    }

    /// <summary>获取所有不重复的标签</summary>
    [HttpGet("tags")]
    public async Task<IActionResult> GetAllTags()
    {
        var tags = await _context.NoteTags
            .Select(t => t.TagId)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();

        return Ok(tags);
    }

    /// <summary>创建笔记</summary>
    [HttpPost]
    public async Task<IActionResult> CreateNote([FromBody] CreateNoteRequest request)
    {
        var note = new Note
        {
            Title = request.Title ?? "未命名笔记",
            Content = request.Content ?? "",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.Notes.Add(note);
        await _context.SaveChangesAsync();

        if (request.TagIds != null && request.TagIds.Count > 0)
        {
            foreach (var tagId in request.TagIds)
            {
                _context.NoteTags.Add(new NoteTag
                {
                    NoteId = note.Id,
                    TagId = tagId
                });
            }
            await _context.SaveChangesAsync();
        }

        note = await _context.Notes
            .Include(n => n.Tags)
            .FirstAsync(n => n.Id == note.Id);

        var result = new
        {
            id = note.Id,
            title = note.Title,
            content = note.Content,
            tagIds = note.Tags.Select(t => t.TagId).ToList(),
            createdAt = note.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
            updatedAt = note.UpdatedAt.ToString("yyyy-MM-dd HH:mm")
        };

        return Created($"/api/notes/{note.Id}", result);
    }

    /// <summary>更新笔记</summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateNote(int id, [FromBody] UpdateNoteRequest request)
    {
        var note = await _context.Notes
            .Include(n => n.Tags)
            .FirstOrDefaultAsync(n => n.Id == id);

        if (note == null)
            return NotFound(new { message = "笔记不存在" });

        if (request.Title != null)
            note.Title = request.Title;
        if (request.Content != null)
            note.Content = request.Content;

        note.UpdatedAt = DateTime.Now;

        if (request.TagIds != null)
        {
            var existingTags = await _context.NoteTags.Where(t => t.NoteId == id).ToListAsync();
            _context.NoteTags.RemoveRange(existingTags);

            foreach (var tagId in request.TagIds)
            {
                _context.NoteTags.Add(new NoteTag
                {
                    NoteId = note.Id,
                    TagId = tagId
                });
            }
        }

        await _context.SaveChangesAsync();

        note = await _context.Notes
            .Include(n => n.Tags)
            .FirstAsync(n => n.Id == note.Id);

        var result = new
        {
            id = note.Id,
            title = note.Title,
            content = note.Content,
            tagIds = note.Tags.Select(t => t.TagId).ToList(),
            createdAt = note.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
            updatedAt = note.UpdatedAt.ToString("yyyy-MM-dd HH:mm")
        };

        return Ok(result);
    }

    /// <summary>删除笔记</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNote(int id)
    {
        var note = await _context.Notes.FindAsync(id);
        if (note == null)
            return NotFound(new { message = "笔记不存在" });

        _context.Notes.Remove(note);
        await _context.SaveChangesAsync();

        return Ok(new { message = "删除成功" });
    }
}

public class CreateNoteRequest
{
    public string? Title { get; set; }
    public string? Content { get; set; }
    public List<string>? TagIds { get; set; }
}

public class UpdateNoteRequest
{
    public string? Title { get; set; }
    public string? Content { get; set; }
    public List<string>? TagIds { get; set; }
}
