using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FindTime.Models;

public class Category
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int CategoryId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(7)] // Hex color code
    public string Color { get; set; } = "#000000";

    [Required]
    public int GroupId { get; set; }

    [Required]
    public string CreatedByUserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(GroupId))]
    public virtual Group Group { get; set; } = null!;

    [ForeignKey(nameof(CreatedByUserId))]
    public virtual ApplicationUser CreatedBy { get; set; } = null!;

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();
}