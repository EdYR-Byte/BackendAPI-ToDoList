using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BackendAPI.Models
{
    public class TaskModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int? TaskId { get; set; }
        public int? ProjectId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
        public byte? Priority { get; set; }
        public bool? IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Relacion con Project

        [JsonIgnore]
        [ForeignKey("ProjectId")]
        public ProjectModel? Project { get; set; }

    }
}