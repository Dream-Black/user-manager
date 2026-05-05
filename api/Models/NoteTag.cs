using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ProjectHub.Api.Models;

public class NoteTag
{
    [Key]
    public int Id { get; set; }

    public int NoteId { get; set; }

    [Required]
    [MaxLength(50)]
    public string TagId { get; set; } = string.Empty;

    [ForeignKey("NoteId")]
    [JsonIgnore]
    public virtual Note? Note { get; set; }
}
